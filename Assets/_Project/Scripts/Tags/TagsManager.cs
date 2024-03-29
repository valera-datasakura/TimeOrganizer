using System;
using System.Collections;
using System.Collections.Generic;
using Doozy.Engine;
using UnityEngine;
using Zenject;

namespace TimeOrganizer.Tags
{
    public class TagsManager : TabManager
    {
        [Inject] private List<TagInfo> m_defaultTags;
        [Inject] private ManageTagPanel m_manageTagPanel;

        private void Awake()
        {
            List<TagInfo> tagsToCreate = GetListOfObjects("TAGS_DATA_LOCAL", m_defaultTags);
            tagsToCreate.ForEach(obj => CreateTag(obj, m_content));
        }

        public void CreateTag(TagInfo tagInfo, Transform parent)
        {
            GameObject tagGO = Instantiate(m_prefabs.tagPrefab, parent);
            Tag tagComp = tagGO.GetComponent<Tag>();
            SetTagReferences(tagComp, tagInfo);
            tagComp.Button.OnClick.OnTrigger.Event.AddListener( () => ShowTagEditionPanel(tagComp) );
            
            m_objectsHandler.Tags.Add(tagComp);
        }

        private void CreateCustomTag()
        {
            TagInfo playerInputTag = m_manageTagPanel.GetCurrentTagInfo();

            if (CanSaveTag(playerInputTag) == false) return;
            
            CreateTag(playerInputTag, m_content);
            SaveCurrentTags();
            GameEventMessage.SendEvent("GoToTags");
        }
        
        private bool CanSaveTag(TagInfo playerInputTag)
        {
            bool incorrectInput = string.IsNullOrWhiteSpace(playerInputTag.Label);
            
            bool nameExists = m_objectsHandler.Tags.Exists(t => t.Label == playerInputTag.Label);
            bool alreadyExists = m_manageTagPanel.IsNewTab ? nameExists 
                : playerInputTag.Label != m_manageTagPanel.Item.Label && nameExists;
            
            if (incorrectInput || alreadyExists)
            {
                m_manageTagPanel.InputField.text = "";
                m_manageTagPanel.PlaceholderText.text = incorrectInput ? "Incorrect input" : "Tag already exists";
                return false;
            }
            
            return true;
        }
        
        public void ShowTagCreationPanel()
        {
            m_manageTagPanel.IsNewTab = true;
            
            m_manageTagPanel.Title.text = "New tag";
            m_manageTagPanel.InputField.text = "";
            m_manageTagPanel.ChooseColor = Color.gray;
            m_manageTagPanel.ChooseIconBg.color = Color.gray;
            m_manageTagPanel.DeleteItemButton.gameObject.SetActive(false);
            
            m_manageTagPanel.NextButton.OnClick.OnTrigger.Event.RemoveAllListeners();
            m_manageTagPanel.NextButton.OnClick.OnTrigger.Event.AddListener(CreateCustomTag);
            
            GameEventMessage.SendEvent("GoToManageTag");
        }
        
        private void ShowTagEditionPanel(Tag tagComp)
        {
            m_manageTagPanel.Item = tagComp;
            m_manageTagPanel.IsNewTab = false;
            
            ColorUtility.TryParseHtmlString( "#" + tagComp.Color, out Color col);
            m_manageTagPanel.ChooseColor = col;
            m_manageTagPanel.ChooseIconBg.color = col;
            m_manageTagPanel.ChooseIcon.sprite = tagComp.Icon;
            m_manageTagPanel.ChooseIconId = tagComp.IconID;
            
            m_manageTagPanel.Title.text = "Edit tag";
            m_manageTagPanel.InputField.text = tagComp.Label;
            m_manageTagPanel.DeleteItemButton.gameObject.SetActive(true);

            m_manageTagPanel.NextButton.OnClick.OnTrigger.Event.RemoveAllListeners();
            m_manageTagPanel.NextButton.OnClick.OnTrigger.Event.AddListener(ApplyChanges);
            
            GameEventMessage.SendEvent("GoToManageTag");
        }

        private void ApplyChanges()
        {
            TagInfo playerInputTag = m_manageTagPanel.GetCurrentTagInfo();
            
            if (CanSaveTag(playerInputTag) == false) return;

            m_manageTagPanel.Item.Label = playerInputTag.Label;
            m_manageTagPanel.Item.Color = playerInputTag.Color;
            m_manageTagPanel.Item.Icon = m_tagSprites[playerInputTag.SpriteID].sprite;
            m_manageTagPanel.Item.IconID = playerInputTag.SpriteID;

            SaveCurrentTags();
            GameEventMessage.SendEvent("GoToTags");
        }
        
        public void DeleteTag()
        {
            Tag tagItem = m_objectsHandler.Tags.Find(t => t.Label == m_manageTagPanel.Item.Label);
            m_objectsHandler.Tags.Remove(tagItem);
            Destroy(tagItem.gameObject);

            SaveCurrentTags();
            GameEventMessage.SendEvent("GoToTags");
        }
        
        private void SaveCurrentTags()
        {
            ListSerializer.SaveList(ConvertToTagInfos(m_objectsHandler.Tags), "TAGS_DATA_LOCAL");
        }
        
        private List<TagInfo> ConvertToTagInfos(List<Tag> tagsToConvert)
        {
            List<TagInfo> tagInfos = new List<TagInfo>();
            foreach (Tag tagComp in tagsToConvert)
            {
                tagInfos.Add(new TagInfo
                {
                    Color = tagComp.Color,
                    Label = tagComp.Label,
                    SpriteID = tagComp.IconID
                });
            }
            return tagInfos;
        }
    }
}
