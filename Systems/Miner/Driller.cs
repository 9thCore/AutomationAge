using UnityEngine;
using System.Collections;
using UWE;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using Nautilus.Utility;

namespace AutomationAge.Systems.Miner
{
    internal class Driller : AttachableModule
    {
        public const float HitTime = 1f;
        public const float MinePowerConsumption = 2.5f;
        public readonly Quaternion HitFXRotation = Quaternion.Euler(new Vector3(270f, 0f, 0f));

        private BaseMiner _miner;
        public BaseMiner Miner
        {
            get
            {
                if (_miner == null)
                {
                    _miner = ModuleAttachedTo.GetComponent<BaseMiner>();
                }
                return _miner;
            }
        }

        private GameObject _container;
        public GameObject Container => _container ??= transform.Find("Container").gameObject;

        private StorageContainer _storage;
        public StorageContainer Storage => _storage ??= transform.Find("Container").gameObject.GetComponent<StorageContainer>();

        private Coroutine coroutine;

        public override void OnAttach(GameObject module)
        {
            if (module.TryGetComponent(out BaseMiner miner))
            {
                Miner.drillAttachment = this;
                Miner.OnRockSpawn += OnRockSpawn;
            } else
            {
                Plugin.Logger.LogWarning("Driller was built on something other than an extruder?? Out of here!!");
                Destroy(gameObject);
            }
        }

        public override void StartBehaviour()
        {
            Container.SetActive(true);

            if (Miner.spawnedRock == null) { return; }
            OnRockSpawn();
        }

        public override void StopBehaviour()
        {
            Container.SetActive(false);

            if (coroutine == null) { return; }
            CoroutineHost.StopCoroutine(coroutine);
        }

        public override void RemoveAttachable()
        {
            Miner.OnRockSpawn -= OnRockSpawn;
            Miner.drillAttachment = null;
        }

        public bool GetWaitTime(out float waitTime)
        {
            if (Miner.rockSpawnTimer >= BaseMiner.RockSpawnTime)
            {
                waitTime = 0f;
                return false;
            }

            waitTime = BaseMiner.RockSpawnTime - Miner.rockSpawnTimer;
            return true;
        }

        public bool CanMine()
        {
            if (Storage.container.IsFull()) { return false; }

            if (GameModeUtils.RequiresPower())
            {
                if (Miner.powerRelay == null) { return false; }
                return Miner.powerRelay.GetPower() > MinePowerConsumption;
            }

            return true;
        }

        public bool ConsumePower()
        {
            if (!CanMine()) { return false; }

            if (GameModeUtils.RequiresPower())
            {
                if (Miner.powerRelay == null) { return false; }
                Miner.powerRelay.ConsumeEnergy(MinePowerConsumption, out _);
            }

            return true;
        }

        public void OnRockSpawn()
        {
            if (Miner.spawnedPickupable)
            {
                coroutine = CoroutineHost.StartCoroutine(PickUpDelayed(Miner.spawnedPickupable));
            } else
            {
                if (Miner.spawnedRock.TryGetComponent(out BreakableResource breakableResource))
                {
                    coroutine = CoroutineHost.StartCoroutine(BreakChunk(breakableResource));
                } else
                {
                    Plugin.Logger.LogWarning($"No idea what to do with {Miner.spawnedRock.name}! It's neither a breakable nor a pickupable");
                }
            }
        }

        public IEnumerator PickUpDelayed(Pickupable pickupable)
        {
            yield return new WaitForSeconds(HitTime);

            float waitTime;
            if(!GetWaitTime(out waitTime)) { yield return new WaitForSeconds(waitTime); }

            PickUpResource(pickupable);
        }

        public void PickUpResource(Pickupable pickupable)
        {
            if (!Storage.container.HasRoomFor(pickupable))
            {
                pickupable.Drop(transform.position + transform.forward, default, false);
                return;
            }

            Storage.container.AddItem(pickupable);
            Miner?.PickedUp();
        }

        public void CatchUp(GameObject rock)
        {
            if (rock.TryGetComponent(out BreakableResource chunk))
            {
                CoroutineHost.StartCoroutine(GetChunkResources(chunk));
            } else if (rock.TryGetComponent(out Pickupable pickupable))
            {
                PickUpResource(pickupable);
            } else
            {
                Plugin.Logger.LogError($"Cannot catch up with {rock}, as it's neither a breakable resource nor a pickupable!");
            }
        }

        public IEnumerator BreakChunk(BreakableResource chunk)
        {
            float waitTime;
            if (!GetWaitTime(out waitTime)) { yield return new WaitForSeconds(waitTime); }

            while (chunk.hitsToBreak > 1)
            {
                yield return new WaitForSeconds(HitTime);

                if (ConsumePower())
                {
                    chunk.HitResource();
                    FMODUWE.PlayOneShot(chunk.hitSound, chunk.transform.position);
                    if (chunk.hitFX != null)
                    {
                        Utils.PlayOneShotPS(chunk.hitFX, chunk.transform.position, HitFXRotation);
                    }
                }
            }

            yield return new WaitForSeconds(HitTime);
            yield return new WaitUntil(ConsumePower);

            chunk.broken = true;
            SendMessage("OnBreakResource", null, SendMessageOptions.DontRequireReceiver);
            if (chunk.gameObject.GetComponent<VFXBurstModel>() != null)
            {
                chunk.gameObject.BroadcastMessage("OnKill");
            }
            else
            {
                Destroy(chunk.gameObject);
            }

            FMODUWE.PlayOneShot(chunk.breakSound, chunk.transform.position);
            if (chunk.hitFX != null)
            {
                Utils.PlayOneShotPS(chunk.breakFX, chunk.transform.position, HitFXRotation);
            }

            yield return GetChunkResources(chunk);
        }

        public IEnumerator GetChunkResources(BreakableResource chunk)
        {
            List<AssetReferenceGameObject> gameObjects = new List<AssetReferenceGameObject>();
            for (int i = 0; i < chunk.numChances; i++)
            {
                AssetReferenceGameObject go = chunk.ChooseRandomResource();
                if (go != null)
                {
                    gameObjects.Add(go);
                }
            }

            if (gameObjects.Count == 0)
            {
                gameObjects.Add(chunk.defaultPrefabReference);
            }

            foreach (AssetReferenceGameObject go in gameObjects)
            {
                CoroutineTask<GameObject> result = AddressablesUtility.InstantiateAsync(go.RuntimeKey as string, null, chunk.transform.position + chunk.transform.up * chunk.verticalSpawnOffset);
                yield return result;

                GameObject result2 = result.GetResult();
                if (result2 == null)
                {
                    Plugin.Logger.LogError($"Could not spawn {go.RuntimeKey as string}");
                    continue;
                }

                if (result2.TryGetComponent(out Pickupable pickupable))
                {
                    PickUpResource(pickupable);
                }
                else
                {
                    Plugin.Logger.LogError($"Could not pick up {result2.name}");
                }
            }
        }

    }
}
