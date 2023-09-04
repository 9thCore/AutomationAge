using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutomationAge.Systems
{
    internal class AttachableModule : MonoBehaviour
    {
        // misnomer it actually searches in a box lol
        private static readonly Vector3 SearchRadius = new Vector3(1f, 1f, 1f);

        public string attachedID = null;
        public Vector3 attachedPos = Vector3.zero;

        public virtual void OnAttach(GameObject module) { }

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
            OnAttach(module);

            Save();
        }

        public void Attach()
        {

            GameObject module = ConstructableOnSpecificModules.attachedModule;
            if (module != null)
            {
                // Constructed just now, so we have a module we can attach to
                AttachToModule(module);
            }
            else
            {
                // Constructed last session, so we don't have a reference to the attached module
                // Attempt to find module to re-attach to
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
                        return;
                    }
                }
                
                Plugin.Logger.LogError($"Could not reattach {gameObject.name} at {transform.position} to container id {attachedID}");
            }
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
            attachableSaveData[id] = new AttachableSaveData(this);
        }
    }
}
