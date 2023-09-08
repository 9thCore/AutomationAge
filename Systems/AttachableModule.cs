using AutomationAge.Systems.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems
{
    public class AttachableModule : MonoBehaviour
    {
        // misnomer it actually searches in a box lol
        private static readonly Vector3 SearchRadius = new Vector3(1f, 1f, 1f);

        internal NetworkContainer container;
        public string attachedID = null;
        public Vector3 attachedPos = Vector3.zero;
        public bool fullyConstructed = false;

        private bool firstRun = true;

        public virtual void OnAttach(GameObject module) { }
        public virtual void StartBehaviour() { }
        public virtual void StopBehaviour() { }
        public virtual void RemoveAttachable() { }
        public virtual void SaveData(string id) { }
        public virtual void LoadSaveData(string id) { }
        public virtual void RemoveSaveData(string id) { }

        public void AttachToModule(GameObject module)
        {
            if (module == null)
            {
                throw new ArgumentNullException("module");
            }

            if (!module.TryGetComponent(out PrefabIdentifier identifier))
            {
                throw new ArgumentException($"Attached module {module} does not have a PrefabIdentifier!");
            }

            attachedID = identifier.id;
            attachedPos = module.transform.position;
            container = module.EnsureComponent<NetworkContainer>();
            OnAttach(module);

            CoroutineHost.StartCoroutine(DelayedSave());
        }

        public void Start()
        {
            if (container == null) { return; }
        }

        public void OnEnable()
        {
            if (container == null) { return; }
            if (firstRun) { return; }
            StartBehaviour();
        }

        public void OnDisable()
        {
            if (container == null) { return; }
            if (firstRun)
            {
                firstRun = false;
                return;
            }
            StopBehaviour();
        }

        public void OnDestroy()
        {
            RemoveAttachable();
            Unsave();
        }

        public void Awake()
        {
            // If this was just constructed, then we can immediately attach
            GameObject module = ConstructableOnSpecificModules.attachedModule;
            if (module == null)
            {
                // Was not just constructed, so have to do some more trickery
                firstRun = false;
                CoroutineHost.StartCoroutine(Attach());
                return;
            }

            AttachToModule(module);
        }

        public IEnumerator Attach()
        {
            // Constructed last session, so we don't have a reference to the attached module
            // Attempt to find module to re-attach to

            // Wait until the prefab identifier is available
            if (!gameObject.TryGetComponent(out PrefabIdentifier identifier1)) { yield break; }
            yield return new WaitUntil(() => !string.IsNullOrEmpty(identifier1.id));

            Load();

            // Race condition? Not sure, but seems to fix issues relating to not finding the attachment
            yield return new WaitForEndOfFrame();

            Collider[] colliders = Physics.OverlapBox(attachedPos, SearchRadius);

            for (int i = 0; i < colliders.Length; i++)
            {
                GameObject obj = colliders[i].gameObject;
                GameObject parent = obj.transform.parent.gameObject;
                if (parent == null) { continue; }

                if (parent.TryGetComponent(out PrefabIdentifier identifier) && identifier.id == attachedID)
                {
                    AttachToModule(parent);
                    if(fullyConstructed) { StartBehaviour(); }
                    yield break;
                }
            }

            Plugin.Logger.LogError($"Could not reattach {gameObject.name} at {transform.position} to container id {attachedID}. Out!");
            Destroy(gameObject);
        }

        public void Unsave()
        {
            if (gameObject.TryGetComponent(out PrefabIdentifier identifier))
            {
                Dictionary<string, AttachableSaveData> attachableSaveData = SaveHandler.data.attachableSaveData;
                attachableSaveData.Remove(identifier.id);
                RemoveSaveData(identifier.id);
                return;
            }

            Plugin.Logger.LogError($"Attachable {gameObject.name} does not have a PrefabIdentifier?? Cannot remove saved data!!");
        }

        public void Load()
        {
            if(gameObject.TryGetComponent(out PrefabIdentifier identifier)) {
                Dictionary<string, AttachableSaveData> attachableSaveData = SaveHandler.data.attachableSaveData;
                if (attachableSaveData.TryGetValue(identifier.id, out AttachableSaveData data))
                {
                    data.LoadAttachableData(this);
                }
                LoadSaveData(identifier.id);
                return;
            }

            Plugin.Logger.LogError($"Attachable {gameObject.name} does not have a PrefabIdentifier?? Cannot load data!!");
        }

        public void Save()
        {
            string id = gameObject.GetComponent<PrefabIdentifier>().id; if (gameObject.TryGetComponent(out PrefabIdentifier identifier))
            {
                Dictionary<string, AttachableSaveData> attachableSaveData = SaveHandler.data.attachableSaveData;
                attachableSaveData[id] = new AttachableSaveData(this);
                SaveData(identifier.id);
                return;
            }

            Plugin.Logger.LogError($"Attachable {gameObject.name} does not have a PrefabIdentifier?? Cannot save data!!");
        }

        public IEnumerator DelayedSave()
        {
            if (!gameObject.TryGetComponent(out PrefabIdentifier identifier)) { yield break; }
            yield return new WaitUntil(() => !string.IsNullOrEmpty(identifier.id));
            Save();
        }
    }
}
