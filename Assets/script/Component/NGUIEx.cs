/*
               #########                       
              ############                     
              #############                    
             ##  ###########                   
            ###  ###### #####                  
            ### #######   ####                 
           ###  ########## ####                
          ####  ########### ####               
         ####   ###########  #####             
        #####   ### ########   #####           
       #####   ###   ########   ######         
      ######   ###  ###########   ######       
     ######   #### ##############  ######      
    #######  #####################  ######     
    #######  ######################  ######    
   #######  ###### #################  ######   
   #######  ###### ###### #########   ######   
   #######    ##  ######   ######     ######   
   #######        ######    #####     #####    
    ######        #####     #####     ####     
     #####        ####      #####     ###      
      #####       ###        ###      #        
        ###       ###        ###               
         ##       ###        ###               
__________#_______####_______####______________

                我们的未来没有BUG              
* ==============================================================================
* Filename: NGUIEx
* Created:  2017/8/26 16:59:51
* Author:   HaYaShi ToShiTaKa
* Purpose:  
* ==============================================================================
*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NGUIEx {
    /// <summary>
    /// 注意：能不用这个接口，尽量不用这个接口（聊天之类的可能用到，其他的尽量不用）
    /// 支持大量item滑动显示的 table创建，左右 上下箭头有效，有对象池
    /// FillItem的时候要重置item,（注销点击，一些图片显示的重置）
    /// 每一次调用这个函数都会把所有的子节入池，然后出池使用
    /// 获取item使用TableItemList,直接用数据索引scrollItemList[index]可以范围到对应的item，为空说明没被绘制
    /// </summary>
    /// <returns></returns>
    static public void CreateScrollView<T>(this UITable table, GameObject templateItem, IList datas, TableItemList<T> scrollItemList, MonoBehaviour parentUI)
        where T : TableBaseItem, new() {
        if (scrollItemList == null) {
            scrollItemList = new TableItemList<T>(null, parentUI);
        }

        UIScrollView scrollView = NGUITools.FindInParents<UIScrollView>(table.gameObject);
        //scrollView.StopDragMove();
        UIScrollView.Movement moveType = table.columns == 0 ? UIScrollView.Movement.Horizontal : UIScrollView.Movement.Vertical;
        UITable.Direction direction = table.direction;
        int itemCount = datas.Count;//grid的行(列)数量
        Bounds b = NGUIMath.CalculateRelativeWidgetBounds(templateItem.transform, true);
        
        Vector2 padding = table.padding;
        int cellHeight = (int)b.size.y + (int)padding.y;
        int cellWidth = (int)b.size.x;
        int fillCount = 0; //当前scrollView被填满的格子数
        int cacheNum = 3; //多出来的缓存格子
        Vector3 lastPos = Vector3.zero;
        UIPanel panel = scrollView.GetComponent<UIPanel>();

        panel.onClipMove = null;
        if (moveType == UIScrollView.Movement.Vertical) {
            fillCount = Mathf.CeilToInt(panel.height / cellHeight);
        }
        else if (moveType == UIScrollView.Movement.Horizontal) {
            fillCount = Mathf.CeilToInt(panel.width / cellWidth);
        }
        scrollItemList.ResetPos();
        scrollItemList.table = table;
        scrollItemList.panel = panel;
        scrollItemList.scrollView = scrollView;
        scrollItemList.CreateItemPool(templateItem, fillCount + cacheNum);
        List<T> scrollItems = scrollItemList.items;
        if (scrollItems == null) {
            scrollItems = new List<T>();
            scrollItemList.items = scrollItems;
        }
        else {
            scrollItemList.StoreAllItem();
        }

        // 如果item数量大于填满显示面板的数量做优化
        if (itemCount >= fillCount + cacheNum) {
            itemCount = fillCount + cacheNum;
            scrollItemList.lastIndex = 0; //上次显示出来的第一个格子，在grid数据中的index
            int maxIndex = itemCount - 1;
            int minIndex = 0;
            int forwardCacheNum = 0;//用于缓存向指定方向滑动，预加载的格子数
                                    // 拖拽刷新面板
            panel.onClipMove = (uiPanel) => {
                Vector3 delata = lastPos - panel.transform.localPosition;
                float distance = -1;
                int index = 0;//当前显示出来的第一个格子，在grid数据中的index
                float curItemDistance = 0;

                distance = delata.y != 0 ? delata.y : delata.x;

                // 满的时候向上滑不管它
                if (distance > 0 && direction == UITable.Direction.Down) {
                    return;
                }
                if (distance < 0 && direction == UITable.Direction.Up) {
                    return;
                }
                distance = Mathf.Abs(distance);

                curItemDistance = CalItemDistance(moveType, scrollItems[scrollItems.Count - 1].transform.localPosition);
                if (curItemDistance < distance) {
                    index = Mathf.Min(scrollItems[scrollItems.Count - 1].index + 1, datas.Count - 1);
                }
                else {
                    for (int i = 0; i < scrollItems.Count; i++) {
                        Vector3 tmpVec = scrollItems[i].transform.localPosition;
                        curItemDistance = CalItemDistance(moveType, tmpVec);
                        if (curItemDistance >= distance) {
                            index = Mathf.Max(scrollItems[i].index - 1, 0);
                            break;
                        }
                    }
                }
                // 拖拽不满一个单元格
                if (index == scrollItemList.lastIndex) return;
                // 拉到底了
                if (index + itemCount > datas.Count) {
                    if (scrollItemList.lastIndex + itemCount == datas.Count) {
                        return;
                    }
                    else {
                        index = datas.Count - itemCount;
                    }
                }

                // 重刷
                int offset = Math.Abs(index - scrollItemList.lastIndex);

                // 判断要把最上（左）的item移动到最下（右）,还是相反
                if (scrollItemList.lastIndex < index) {
                    //如果有上一次的缓存数量，就清掉
                    if (forwardCacheNum > 0) {
                        while (forwardCacheNum > 1) {
                            //上（左）移动到下（右）
                            MoveTableItem<T>(scrollItems, moveType, datas, ref minIndex, ref maxIndex, ref forwardCacheNum, true, true, direction, padding);
                        }

                    }
                    // 滑到底的时候，把上部缓存的那一个item移动到下部
                    if ((forwardCacheNum > 0 && index + itemCount == datas.Count)) {
                        //上（左）移动到下（右）
                        MoveTableItem<T>(scrollItems, moveType, datas, ref minIndex, ref maxIndex, ref forwardCacheNum, true, true, direction, padding);
                    }

                    for (int i = 1; i <= offset; i++) {
                        //上（左）移动到下（右）
                        MoveTableItem<T>(scrollItems, moveType, datas, ref minIndex, ref maxIndex, ref forwardCacheNum, true, false, direction, padding);
                    }

                }
                else {
                    forwardCacheNum = forwardCacheNum - offset;
                    //缓存数量
                    int targetNum = direction == UITable.Direction.Down ? cacheNum - 1 : cacheNum - 2;
                    while ((forwardCacheNum < targetNum && index >= targetNum) || (forwardCacheNum < 0 && index < targetNum)) {
                        // 下（右）移动到上（左)
                        MoveTableItem<T>(scrollItems, moveType, datas, ref minIndex, ref maxIndex, ref forwardCacheNum, false, true, direction, padding);
                    }
                }
                scrollItemList.lastIndex = index;

            };
        }

        // 添加能填满UI数量的button
        for (int i = 0; i < itemCount; i++) {
            T item = scrollItemList.GetGridItem();
            item.transform.parent = table.transform;
            item.gameObject.SetActive(true);
            item.table = table;
            item.itemCount = itemCount;
            item.parentUI = parentUI;
            scrollItems.Add(item);
            item.FillItem(datas, i);
        }
        scrollItemList.datas = datas;
        scrollItemList.itemCount = fillCount + cacheNum;
        lastPos = panel.transform.localPosition;

        if (scrollView != null && !scrollView.disableDragIfFits) {
            Bounds tableBound;
            Bounds itemBound;

            moveType = scrollView.movement;
            scrollView.onMomentumMove = null;
            scrollView.onDragFinished = null;
            Vector3 lastPosX = panel.transform.localPosition;
            scrollView.onMomentumMove += () => {
                if (moveType == UIScrollView.Movement.Vertical) {
                    tableBound = NGUIMath.CalculateRelativeWidgetBounds(table.transform, false);
                    itemBound = NGUIMath.CalculateRelativeWidgetBounds(templateItem.transform, false);
                    if (tableBound.size.y + itemBound.size.y * 0.5f < panel.height) {
                        SpringPanel.Begin(panel.gameObject, lastPosX, 13f).strength = 8f;
                    }
                }
            };
            scrollView.onDragFinished += () => {
                if (moveType == UIScrollView.Movement.Vertical) {
                    tableBound = NGUIMath.CalculateRelativeWidgetBounds(table.transform, false);
                    itemBound = NGUIMath.CalculateRelativeWidgetBounds(templateItem.transform, false);
                    if (tableBound.size.y + itemBound.size.y * 0.5f < panel.height) {
                        SpringPanel.Begin(panel.gameObject, lastPosX, 13f).strength = 8f;
                    }
                }
            };
        }
        scrollItemList.StoreQueuePoolItem();
        table.Reposition();
    }
    static private float CalItemDistance(UIScrollView.Movement moveType, Vector3 postion) {
        float moveLength = 0;
        if (moveType == UIScrollView.Movement.Horizontal) {
            moveLength = Math.Abs(postion.x);
        }
        else {
            moveLength = Math.Abs(postion.y);
        }
        return moveLength;
    }
    static public void MoveTableItem<T>(
        List<T> scrollItems, UIScrollView.Movement moveType, IList datas, ref int minIndex, ref int maxIndex,
        ref int forwardCacheNum, bool isTopToBottom, bool isMoveForward, UITable.Direction direction, Vector2 padding) where T : TableBaseItem {
        T item;
        // 判断是否是 上（左）移动到下（右)
        int curIndex;
        int itemIndex;
        int sign;
        if (isTopToBottom) {
            curIndex = maxIndex + 1;
            itemIndex = 0;
            sign = 1;
        }
        else {
            curIndex = minIndex - 1;
            itemIndex = scrollItems.Count - 1;
            sign = -1;
        }
        item = scrollItems[itemIndex];

        int targetIndex = itemIndex == 0 ? scrollItems.Count - 1 : 0;
        T targetItem = scrollItems[targetIndex];
        Vector3 targetPos = targetItem.transform.localPosition;

        scrollItems.Remove(item);
        scrollItems.Insert(targetIndex, item);

        item.FillItem(datas, curIndex);
        Bounds b;

        if (direction == UITable.Direction.Down) {
            if (isTopToBottom) {
                b = NGUIMath.CalculateRelativeWidgetBounds(targetItem.transform, false);
            }
            else {
                b = NGUIMath.CalculateRelativeWidgetBounds(item.transform, false);
            }
        }
        else {
            if (!isTopToBottom) {
                b = NGUIMath.CalculateRelativeWidgetBounds(targetItem.transform, false);
            }
            else {
                b = NGUIMath.CalculateRelativeWidgetBounds(item.transform, false);
            }
        }

        float cellHeight = b.size.y + padding.y;
        float cellWidth = b.size.x + padding.x;

        ReSetCellPostion<T>(item, moveType, targetPos, isTopToBottom, cellWidth, cellHeight, direction);


        minIndex += sign;
        maxIndex += sign;
        if (isMoveForward) {
            forwardCacheNum -= sign;
        }

    }
    static private void ReSetCellPostion<T>(T item, UIScrollView.Movement moveType, Vector3 pos, bool isTopToBottom, float cellWidth, float cellHeight, UITable.Direction direction) where T : TableBaseItem {
        int sign = 1;
        if (direction == UITable.Direction.Down) {
            if (isTopToBottom) {
                sign = -1;
            }
            else {
                sign = 1;
            }
        }
        else {
            if (!isTopToBottom) {
                sign = -1;
            }
            else {
                sign = 1;
            }
        }

        if (moveType == UIScrollView.Movement.Horizontal) {
            item.transform.localPosition = new Vector3(pos.x + sign * cellWidth, pos.y, 0);
        }
        else if (moveType == UIScrollView.Movement.Vertical) {
            item.transform.localPosition = new Vector3(pos.x, pos.y + sign * cellHeight, 0);
        }
    }
}
