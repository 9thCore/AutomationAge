using System;
using UnityEngine;

namespace AutomationAge.Systems
{
    public class AttachableModule : MonoBehaviour, IConstructable
    {
        public enum SpecialModule
        {
            None,
            NuclearReactor,
            BioReactor
        }

        public static readonly Vector3 SearchDistance = new Vector3(1f, 1f, 1f);

        internal AttachableSaveData _saveData;
        internal AttachableSaveData SaveData
        {
            get
            {
                if (_saveData == null)
                {
                    if (!Load())
                    {
                        _saveData = new AttachableSaveData();
                        _saveData.attachedID = string.Empty;
                        _saveData.attachedPos = Vector3.zero;
                        _saveData.fullyConstructed = Constructable.constructed;

                        Save();
                    }
                    _saveData.module = this;
                }
                return _saveData;
            }
        }

        internal Constructable _constructable;
        internal Constructable Constructable => _constructable ??= gameObject.GetComponent<Constructable>();

        internal GameObject _moduleAttachedTo;
        internal GameObject ModuleAttachedTo
        {
            get
            {
                if (_moduleAttachedTo == null)
                {
                    LateAttach();
                }
                return _moduleAttachedTo;
            }
        }

        public virtual void OnAttach(GameObject module) { }
        public virtual void RemoveAttachable() { }
        public virtual void OnSave(string id) { }
        public virtual void OnLoad(string id) { }
        public virtual void OnUnsave(string id) { }
        public virtual void StartBehaviour() { }
        public virtual void StopBehaviour() { }

        public virtual void PostLoad()
        {
            if (SaveData.fullyConstructed) { StartBehaviour(); }
        }

        public void OnEnable()
        {
            if (_moduleAttachedTo == null) { return; }
            StartBehaviour();
        }

        public void OnDisable()
        {
            if (_moduleAttachedTo == null) { return; }
            StopBehaviour();
        }

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

            _moduleAttachedTo = module;

            SaveData.attachedID = identifier.Id;
            SaveData.attachedPos = module.transform.position;

            // Stuff that require special attention, like nuclear reactors
            if (module.TryGetComponent(out BaseNuclearReactor _))
            {
                SaveData.specialModule = SpecialModule.NuclearReactor;
            }
            else if (module.TryGetComponent(out BaseBioReactor _))
            {
                SaveData.specialModule = SpecialModule.BioReactor;
            }

            OnAttach(module);
        }

        public void OnDestroy()
        {
            RemoveAttachable();
            if (Constructable.constructedAmount <= 0f)
            {
                Unsave();
            }
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

        public void LateAttach()
        {
            // Constructed last session, so we don't have a reference to the attached module
            // Attempt to find module to re-attach to
            Collider[] colliders = Physics.OverlapBox(SaveData.attachedPos, SearchDistance);

            for (int i = 0; i < colliders.Length; i++)
            {
                Transform tr = colliders[i].transform;
                while(tr != null)
                {
                    GameObject obj = tr.gameObject;

                    switch (SaveData.specialModule)
                    {
                        case SpecialModule.NuclearReactor:
                            GameObject go = obj.transform.parent.gameObject;
                            if (go.TryGetComponent(out BaseNuclearReactorGeometry geometry))
                            {
                                obj = geometry.GetModule().gameObject;
                            }
                            break;
                        case SpecialModule.BioReactor:
                            GameObject go1 = obj.transform.parent.gameObject;
                            if (go1.TryGetComponent(out BaseBioReactorGeometry geometry1))
                            {
                                obj = geometry1.GetModule().gameObject;
                            }
                            break;
                        default:
                            break;
                    }

                    if (obj.TryGetComponent(out PrefabIdentifier identifier) && identifier.Id == SaveData.attachedID)
                    {
                        AttachToModule(obj);
                        PostLoad();
                        return;
                    }

                    tr = tr.parent;
                }
            }

            Plugin.Logger.LogError($"Could not reattach {gameObject.name} at {transform.position} to module id {SaveData.attachedID}. Out!");
            Destroy(gameObject);
        }

        public void Save()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            SaveHandler.data.attachableSaveData[prefabIdentifier.Id] = SaveData;
            OnSave(prefabIdentifier.Id);
        }

        public bool Load()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            if (SaveHandler.data.attachableSaveData.TryGetValue(prefabIdentifier.Id, out _saveData))
            {
                _saveData.module = this;
                OnLoad(prefabIdentifier.Id);
                return true;
            }

            return false;
        }

        public void Unsave()
        {
            PrefabIdentifier prefabIdentifier = gameObject.GetComponent<PrefabIdentifier>();
            SaveHandler.data.attachableSaveData.Remove(prefabIdentifier.Id);
            OnUnsave(prefabIdentifier.Id);
        }

        public virtual void OnConstructedChanged(bool constructed)
        {
            SaveData.fullyConstructed = constructed;
        }

        public virtual bool IsDeconstructionObstacle()
        {
            return true;
        }

        public virtual bool CanDeconstruct(out string reason)
        {
            reason = string.Empty;
            return true;
        }
    }
}
