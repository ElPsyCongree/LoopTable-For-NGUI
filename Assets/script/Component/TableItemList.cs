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
* Filename: TableItemList
* Created:  2016/2/12 17:30:48
* Author:   HaYaShi ToShiTaKa
* Purpose:  ScrollItem Table类型 的管理类
* ==============================================================================
*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : class, new() {
    public delegate T CreateFunc();

    public ObjectPool() {

    }
    public ObjectPool(int poolSize, CreateFunc createFunc = null, Action<T> resetAction = null) {
        Init(poolSize, createFunc, resetAction);
    }
    public T GetObject() {
        if (m_objStack.Count > 0) {
            T t = m_objStack.Pop();
            if (m_resetAction != null) {
                m_resetAction(t);
            }
            return t;
        }
        return (m_createFunc != null) ? m_createFunc() : new T();
    }

    public void Init(int poolSize, CreateFunc createFunc = null, Action<T> resetAction = null) {
        m_objStack = new Stack<T>();
        m_resetAction = resetAction;
        m_createFunc = createFunc;
        for (int i = 0; i < poolSize; i++) {
            T item = (m_createFunc != null) ? m_createFunc() : new T();
            m_objStack.Push(item);
        }
    }

    public void Store(T obj) {
        if (obj == null)
            return;
        if (m_resetAction != null)
            m_resetAction(obj);
        m_objStack.Push(obj);
    }

    // 少用，调用这个池的作用就没有了
    public void Clear() {
        if (m_objStack != null)
            m_objStack.Clear();
    }

    public int Count {
        get {
            if (m_objStack == null)
                return 0;
            return m_objStack.Count;
        }
    }

    public Stack<T>.Enumerator GetIter() {
        if (m_objStack == null)
            return new Stack<T>.Enumerator();
        return m_objStack.GetEnumerator();
    }

    private Stack<T> m_objStack = null;
    private Action<T> m_resetAction = null;
    private CreateFunc m_createFunc = null;
}

public class ItemListBase {

    #region property
    public IList datas {
        get;
        set;
    }
    public int lastIndex {
        get {
            return m_lastIndex;
        }
        set {
            m_lastIndex = value;
        }
    }
    public MonoBehaviour parentUI {
        get;
        set;
    }
    public GameObject itemTemplate {
        get {
            return m_itemTemplate;
        }
    }
    public UIScrollView scrollView {
        get;
        set;
    }
    public UIPanel panel {
        set {
            if (m_panel != value) {
                m_initScrollPos = value.transform.localPosition;
                m_initOffset = value.clipOffset;
                m_panel = value;
            }
        }
        get {
            return m_panel;
        }
    }
    public Vector3 initScrollPos {
        get {
            return m_initScrollPos;
        }
    }
    public int selectIndex {
        get {
            return m_selectIndex;
        }
        set {
            m_selectIndex = value;
        }
    }
    public bool isNotSelectEmpty {
        get {
            return m_isNotSelectEmpty;
        }
        set {
            m_isNotSelectEmpty = value;
        }
    }
    #endregion

    #region member
    protected Transform m_itemPool;
    protected GameObject m_itemTemplate;
    private int m_selectIndex = -1;
    private bool m_isNotSelectEmpty = true;
    protected int m_lastIndex = 0;
    protected int m_itemCount;
    protected bool m_isFull = false;
    protected UIPanel m_panel;
    protected Vector3 m_initScrollPos;
    protected Vector2 m_initOffset;
    protected Vector3 m_preContainerPos;
    #endregion

}

public class TableItemList<T> : ItemListBase where T : TableBaseItem, new() {

    #region property
    public List<T> items {
        get;
        set;
    }
    public int itemCount {
        set {
            m_itemCount = value;
            m_isFull = IsItemFull();
        }
        get {
            return m_itemCount;
        }
    }
    private UITable m_table;
    public UITable table {
        get {
            return m_table;
        }
        set {
            if (m_table != value) {
                m_table = value;
                m_preContainerPos = m_table.transform.localPosition;
            }
        }
    }
    #endregion

    #region pool
    private ObjectPool<T> m_itemCOPool = new ObjectPool<T>();
    private Queue<T> m_itemQueuePool = new Queue<T>();
    private T CreateItemGO() {
        T itemCO = GameObject.Instantiate(m_itemTemplate).AddComponent<T>();
        itemCO.transform.parent = m_itemPool;
        itemCO.transform.localPosition = Vector3.zero;
        itemCO.transform.localScale = Vector3.one;
        itemCO.gameObject.SetActive(false);
        itemCO.FindItem();
        return itemCO;
    }
    private void Init(T itemCO) {
        itemCO.gameObject.SetActive(false);
        itemCO.transform.parent = m_itemPool;
        itemCO.transform.localPosition = Vector3.zero;
        itemCO.transform.localScale = Vector3.one;
    }
    public void CreateItemPool(GameObject itemTemplate, int poolNum) {
        if (m_itemPool != null) {
            return;
        }

        m_itemTemplate = itemTemplate;
        m_itemPool = new GameObject().transform;
        m_itemPool.name = "pool";
        m_itemPool.parent = m_panel.transform;
        m_itemPool.localPosition = Vector3.zero;
        m_itemPool.localScale = Vector3.one;

        m_itemCOPool.Init(poolNum, CreateItemGO, Init);
    }
    public T GetGridItem() {
        if (m_itemQueuePool.Count > 0) {
            return m_itemQueuePool.Dequeue();
        }
        return m_itemCOPool.GetObject();
    }
    public void StoreAllItem() {
        for (int i = 0; i < items.Count; i++) {
            items[i].transform.parent = m_itemPool;
            m_itemQueuePool.Enqueue(items[i]);
        }
        items.Clear();
    }
    public void StoreQueuePoolItem() {
        while (m_itemQueuePool.Count > 0) {
            m_itemCOPool.Store(m_itemQueuePool.Dequeue());
        }
    }
    #endregion

    #region public
    public void Clear() {
        if (items != null) {
            StoreAllItem();
        }
        datas = null;
        m_itemCount = 0;
        m_isFull = false;
    }
    public TableItemList() : this(null, null) {
    }
    public TableItemList(List<T> items, MonoBehaviour parentUI) {
        this.items = items;
        this.parentUI = parentUI;
    }
    public TYPE GetBaseUI<TYPE>() where TYPE : MonoBehaviour {
        return parentUI as TYPE;
    }
    public int Length { get { return items.Count; } }
    public bool IsItemFull() {
        return items != null && datas.Count >= itemCount;
    }
    public bool RefreshTable() {
        bool result = false;
        if (table == null) {
            return false;
        }

        if (m_isFull && IsItemFull()) {
            UIScrollView scrollView = NGUITools.FindInParents<UIScrollView>(table.gameObject);

            Vector3 pretablePos = table.transform.localPosition;
            Vector3 preScrollPos = scrollView.transform.localPosition;

            for (int i = 0; i < items.Count; i++) {
                items[i].UpdateItem();
                result = true;
            }
            table.transform.localPosition = pretablePos;
            scrollView.transform.localPosition = preScrollPos;
            table.Reposition();
        }

        return result;
    }
    public void ResetPos() {
        if (panel != null) {
            scrollView.transform.localPosition = m_initScrollPos;
            panel.clipOffset = m_initOffset;
            m_table.transform.localPosition = m_preContainerPos;
        }
    }
    #endregion

    #region private
    public T this[int index] {
        get {
            T item = null;
            for (int i = 0; i < Length; i++) {
                if (items[i].index == index) {
                    item = items[i];
                }
            }
            return item;
        }
    }
    #endregion

}