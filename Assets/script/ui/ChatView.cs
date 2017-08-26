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
* Filename: ChatView
* Created:  2017/8/26 16:57:12
* Author:   HaYaShi ToShiTaKa
* Purpose:  
* ==============================================================================
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatView : MonoBehaviour {

    private List<string> m_history = new List<string>();
    private TableItemList<ChatItem> m_tableList = new TableItemList<ChatItem>();

    private string m_inputTips;
    private GameObject m_btnSend;
    private UIInput m_inputText;
    private UITable m_table;
    private GameObject m_template;

    private void Awake() {
        m_btnSend = transform.FindChild("context/submit").gameObject;
        m_inputText = transform.FindChild("context/input").GetComponent<UIInput>();
        m_table = transform.FindChild("context/front/Scroll View/Table").GetComponent<UITable>();
        m_template = transform.FindChild("context/front/Scroll View/Label").gameObject;
    }

    private void Start() {
        m_template.gameObject.SetActive(false);
        m_btnSend.RegistUIButton(SendText);
        RefreshTextList();
    }

    private void SendText(GameObject go) {
        string text = m_inputText.value;
        if (string.IsNullOrEmpty(text)) {
            Debug.Log("text can't be null");
            return;
        }

        m_inputText.value = string.Empty;
        m_history.Add(text);
        RefreshTextList();
    }

    private void RefreshTextList() {
        m_table.CreateScrollView<ChatItem>(m_template, m_history, m_tableList, this);
    }
}

public class ChatItem : TableBaseItem {
    private UILabel m_lb;
    public override void FindItem() {
        base.FindItem();
        m_lb = GetComponent<UILabel>();
    }
    public override void FillItem(IList datas, int index) {
        base.FillItem(datas, index);

        int length = datas.Count;
        string value = datas[length - 1 - index].ToString();
        m_lb.text = value;
    }
}