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

        private bool queuedSave = false;

        public virtual void OnAttach(GameObject module) { }
        public virtual void StartBehaviour() { }
        public virtual void StopBehaviour() { }

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

            Save();
        }

        public void Start()
        {
            if (container == null) { return; }
            if (queuedSave) { Save(); queuedSave = false; } // We can save now, so do that
        }

        public void OnEnable()
        {
            if (container == null) { return; }
            StartBehaviour();
        }

        public void OnDisable()
        {
            if (container == null) { return; }
            StopBehaviour();
        }

        public void Awake()
        {
            // If this was just constructed, then we can immediately attach
            GameObject module = ConstructableOnSpecificModules.attachedModule;
            if (module == null)
            {
                // Was not just constructed, so have to do some more trickery
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

            Collider[] colliders = Physics.OverlapBox(attachedPos, SearchRadius);

            for (int i = 0; i < colliders.Length; i++)
            {
                GameObject obj = colliders[i].gameObject;
                GameObject parent = obj.transform.parent.gameObject;
                if (parent == null) { continue; }

                if (parent.TryGetComponent(out PrefabIdentifier identifier) && identifier.id == attachedID)
                {
                    AttachToModule(parent);
                    yield break;
                }
            }

            Plugin.Logger.LogError($"Could not reattach {gameObject.name} at {transform.position} to container id {attachedID}. Out!");
            Destroy(gameObject);
        }

        public void Load()
        {
            Dictionary<string, AttachableSaveData> attachableSaveData = SaveHandler.data.attachableSaveData;
            string id = gameObject.GetComponent<PrefabIdentifier>().id;
            if (attachableSaveData.TryGetValue(id, out AttachableSaveData data))
            {
                data.LoadAttachableData(this);
            }
        }

        public void Save()
        {
            Dictionary<string, AttachableSaveData> attachableSaveData = SaveHandler.data.attachableSaveData;
            string id = gameObject.GetComponent<PrefabIdentifier>().id;
            if(string.IsNullOrEmpty(id)) { queuedSave = true; return; } // We can't save yet, so queue that for later

            attachableSaveData[id] = new AttachableSaveData(this);
        }
    }
}
