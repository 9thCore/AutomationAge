using Nautilus.Handlers;
using UnityEngine;

namespace AutomationAge.Systems.Blueprint
{
    internal class BlueprintIdentifier : MonoBehaviour
    {
        public const float OverlayAlpha = 0.5f;
        public static readonly Vector3 HalfVector = new Vector3(0.5f, 0.5f);
        public const string EquipmentTypeName = "ItemBlueprint";

        private Pickupable pickupable;
        private PrefabIdentifier identifier;
        private BlueprintSaveData saveData;
        private uGUI_ItemIcon overlayIcon;

        private static EquipmentType equipmentType = EquipmentType.None;
        public static EquipmentType GetEquipmentType()
        {
            if (equipmentType != EquipmentType.None) { return equipmentType; }
            equipmentType = EnumHandler.AddEntry<EquipmentType>(EquipmentTypeName);
            return equipmentType;
        }

        public void LoadSaveIfRequired()
        {
            if (saveData != null) { return; }

            identifier = gameObject.GetComponent<PrefabIdentifier>();
            pickupable = gameObject.GetComponent<Pickupable>();

            if (!Load())
            {
                saveData = new BlueprintSaveData();
                saveData.identifier = this;

                Save();
            }
        }

        public TechType GetTech()
        {
            LoadSaveIfRequired();
            return saveData.CopiedType;
        }

        public void SetTech(TechType tech)
        {
            LoadSaveIfRequired();
            saveData.CopiedType = tech;
            UpdateSprite();
        }

        public void AddOverlay(uGUI_ItemsContainer manager)
        {
            if (HasOverlay()) { return; }
            LoadSaveIfRequired();

            if (!manager.items.TryGetValue(pickupable.inventoryItem, out uGUI_ItemIcon icon))
            {
                Plugin.Logger.LogWarning("Cannot add overlay, as icon was not added to the list??");
                return;
            }

            GameObject overlay = new GameObject();
            uGUI_ItemIcon overlayIcon = overlay.AddComponent<uGUI_ItemIcon>();

            overlayIcon.Init(manager, overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            overlayIcon.SetForegroundAlpha(OverlayAlpha);
            overlay.transform.SetParent(icon.transform);
            overlay.transform.localPosition = new Vector3(icon.backgroundSize.x / 2f, -icon.backgroundSize.y / 2f);
            overlay.transform.localScale = HalfVector;

            this.overlayIcon = overlayIcon;

            UpdateSprite();
        }

        public void RemoveOverlay()
        {
            if (!HasOverlay()) { return; }
            Destroy(overlayIcon.gameObject);
            overlayIcon = null;
        }

        public bool HasOverlay()
        {
            return overlayIcon != null;
        }

        public void UpdateSprite()
        {
            if (!HasOverlay() || saveData.CopiedType == TechType.None) { return; }
            overlayIcon.SetForegroundSprite(SpriteManager.Get(saveData.CopiedType));
        }

        public bool Load()
        {
            if (SaveHandler.data.blueprintSaveData.TryGetValue(identifier.Id, out BlueprintSaveData data))
            {
                saveData = data;
                data.identifier = this;

                return true;
            }
            return false;
        }

        public void Save()
        {
            SaveHandler.data.blueprintSaveData[identifier.Id] = saveData;
        }
    }
}
