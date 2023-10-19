using UnityEngine;

namespace AutomationAge.Systems.Blueprint
{
    internal class BlueprintIdentifier : MonoBehaviour
    {
        public const float OverlayAlpha = 0.5f;
        public static readonly Vector3 HalfVector = new Vector3(0.5f, 0.5f);
        public const string OverlayIconName = "OverlayIcon";

        private Pickupable pickupable;
        private PrefabIdentifier identifier;
        private BlueprintSaveData saveData;
        private uGUI_ItemIcon overlayIcon;

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
            UpdateSprite(overlayIcon, tech);
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

            AddOverlay(manager, icon);
        }

        public void AddOverlay(uGUI_IIconManager manager, uGUI_ItemIcon icon, bool equipment = false)
        {
            overlayIcon = CreateOverlay(manager, icon);
            UpdateSprite(overlayIcon, saveData.CopiedType);

            if (equipment)
            {
                overlayIcon.transform.localScale *= 2f;
                overlayIcon.transform.localPosition = Vector3.zero;
            }
        }

        public static uGUI_ItemIcon CreateOverlay(uGUI_IIconManager manager, uGUI_ItemIcon icon)
        {
            GameObject overlay = new GameObject(OverlayIconName)
            {
                layer = LayerID.UI
            };
            uGUI_ItemIcon overlayIcon = overlay.AddComponent<uGUI_ItemIcon>();
            CanvasGroup group = overlay.AddComponent<CanvasGroup>();

            overlayIcon.Init(manager, overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            overlay.transform.SetParent(icon.transform);
            overlay.transform.localPosition = new Vector3(icon.backgroundSize.x / 2f, -icon.backgroundSize.y / 2f);
            overlay.transform.localScale = HalfVector;
            overlay.transform.localEulerAngles = Vector3.zero;
            group.interactable = false;
            group.blocksRaycasts = false;

            return overlayIcon;
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

        public static void UpdateSprite(uGUI_ItemIcon overlay, TechType tech)
        {
            if (overlay == null || tech == TechType.None) { return; }
            overlay.SetForegroundSprite(null);
            overlay.SetForegroundSprite(SpriteManager.Get(tech));
            overlay.SetForegroundAlpha(OverlayAlpha);
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
