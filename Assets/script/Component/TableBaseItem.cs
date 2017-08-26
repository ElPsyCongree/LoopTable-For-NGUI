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
* Filename: TableBaseItem
* Created:  2016/2/12 17:33:06
* Author:   HaYaShi ToShiTaKa
* Purpose:  Scroll View Table 类型 的item基类
* ==============================================================================
*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableBaseItem : MonoBehaviour {

    #region member
    private IList m_datas;
    private int m_index;
    protected UITable m_table;
    #endregion

    #region property
    public int itemCount {
        get;
        set;
    }//grid填满的数量
    public MonoBehaviour parentUI {
        get;
        set;
    }
    public T GetBaseUI<T>() where T : MonoBehaviour {
        return parentUI as T;
    }
    public int index {
        get {
            return m_index;
        }
        set {
            m_index = value;
        }
    }
    public UITable table {
        set {
            m_table = value;
        }
        get { return m_table; }
    }
    #endregion

    #region public
    public virtual void FindItem() {
    }
    public virtual void FillItem(IList datas, int index) {
        m_datas = datas;
        this.index = index;
        gameObject.UnRegistUIButton();
    }
    public void UpdateItem() {
        if (m_datas == null) return;
        if (m_datas.Count <= 0) return;
        FillItem(m_datas, m_index);
    }
    #endregion
}