using UnityEngine;

namespace AutomationAge.Systems.Blueprint
{
    internal class BlueprintIdentifier : MonoBehaviour
    {
        public PrefabIdentifier Identifier;
        private BlueprintSaveData saveData;

        public void LoadSaveIfRequired()
        {
            if (saveData != null) { return; }

            Identifier = gameObject.GetComponent<PrefabIdentifier>();

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
        }

        public bool Load()
        {
            if (SaveHandler.data.blueprintSaveData.TryGetValue(Identifier.Id, out BlueprintSaveData data))
            {
                saveData = data;
                data.identifier = this;

                return true;
            }
            return false;
        }

        public void Save()
        {
            SaveHandler.data.blueprintSaveData[Identifier.Id] = saveData;
        }

        public void RemoveSave()
        {

        }
    }
}
