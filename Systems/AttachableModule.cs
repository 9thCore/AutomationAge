using AutomationAge.Systems.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UWE;

namespace AutomationAge.Systems
{
    public class AttachableModule : MonoBehaviour
    {
        public enum SpecialModule
        {
            None,
            NuclearReactor,
            BioReactor
        }

        // misnomer it actually searches in a box lol
        private static readonly Vector3 SearchRadius = new Vector3(1f, 1f, 1f);

        private NetworkContainer _container;
        internal NetworkContainer Container {
            get
            {
                if (_container == null) { Attach(); }
                return _container;
            }
        }

        public string attachedID = null;
        public Vector3 attachedPos = Vector3.zero;
        public bool fullyConstructed = false;
        internal SpecialModule specialModule;

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
            _container = module.EnsureComponent<NetworkContainer>();
            OnAttach(module);

            // Stuff that require special attention, like nuclear reactors
            if (module.TryGetComponent(out BaseNuclearReactor _))
            {
                specialModule = SpecialModule.NuclearReactor;
            } else if (module.TryGetComponent(out BaseBioReactor _))
            {
                specialModule = SpecialModule.BioReactor;
            }

            CoroutineHost.StartCoroutine(DelayedSave());
        }

        public void Start()
        {
            if (Container == null) { return; }
        }

        public void OnEnable()
        {
            if (_container == null) { return; }
            if (firstRun) { return; }
            StartBehaviour();
        }

        public void OnDisable()
        {
            if (_container == null) { return; }
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
                // If not, wait to attach lazily after
                return;
            }

            AttachToModule(module);
        }

        private void Attach()
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

                switch (specialModule)
                {
                    case SpecialModule.NuclearReactor:
                        GameObject go = parent.transform.parent.gameObject;
                        if (go.TryGetComponent(out BaseNuclearReactorGeometry geometry))
                        {
                            parent = geometry.GetModule().gameObject;
                        }
                        break;
                    case SpecialModule.BioReactor:
                        GameObject go1 = parent.transform.parent.gameObject;
                        if (go1.TryGetComponent(out BaseBioReactorGeometry geometry1))
                        {
                            parent = geometry1.GetModule().gameObject;
                        }
                        break;
                    default:
                        break;
                }

                if (parent.TryGetComponent(out PrefabIdentifier identifier) && identifier.id == attachedID)
                {
                    firstRun = false;
                    AttachToModule(parent);
                    if(fullyConstructed) { StartBehaviour(); }
                    return;
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
