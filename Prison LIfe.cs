using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using UnityEngine;
using PlayerRoles.FirstPersonControl;

namespace Prison_Life
{
    public enum Timestamp
    {
        lights_out,
        breakfast,
        yardtime,
        lurnch,
        freetime,
        dinner,
        lockdown
    }

    public class Hit
    {
        public Player player;
        public PlayerMovementState Movestate;
        bool CanHit = true;
        PlayerMovementState _Movestate;

        public void Update()
        {
            Movestate = (player.Role.Base as FpcStandardRoleBase).FpcModule.CurrentMovementState;

            if (Movestate != _Movestate)
            {
                _Movestate = Movestate;

                if (Movestate == PlayerMovementState.Sneaking && CanHit)
                {
                    Prison_Life.Instance.OnMelee(player);
                    CanHit = false;
                    MEC.Timing.CallDelayed(1, () => { CanHit = true; });
                }
            }
        }
    }

    public class Gtool : MonoBehaviour
    {
        float St = 0;
        public List<Hit> hits = new List<Hit>();
        string current = $"";

        void Check(Player player)
        {
            if (player.IsDead || player.IsCuffed)
                return;

            Prison_Life pl = Prison_Life.Instance;

            if (Physics.Raycast(player.Position, Vector3.down, out RaycastHit hit, 1, (LayerMask)1))
            {
                string pos = hit.collider.name;
                string[] cz = { "GuardRoom", "Kitchen", "Yard", "Outside Hole", "Elevator to Outside" };

                if (pos == "SpawnFree")
                {
                    if (pl.Prison.Keys.Contains(player.UserId))
                    {
                        pl.Prison.Remove(player.UserId);
                        pl.Free.Add(player.UserId, 0);
                        player.ShowHint("당신은 이제 자유입니다! 붙잡히지 않게 조심하세요!");
                    }
                }
                else if (pos == "SpawnPrison")
                {
                    if (pl.Wander.Keys.Contains(player.UserId) || pl.Prison.Keys.Contains(player.UserId) || pl.Free.Keys.Contains(player.UserId))
                    {

                    }
                    else
                    {
                        pl.Prison.Add(player.UserId, false);
                        player.Kill("곧 수감자로 부활합니다, 준비하세요!");
                    }
                }
                else if (pos == "SpawnWander")
                {
                    if (pl.Wander.Keys.Contains(player.UserId) || pl.Prison.Keys.Contains(player.UserId) || pl.Free.Keys.Contains(player.UserId))
                    {

                    }
                    else
                    {
                        if (pl.Wander.Keys.Count() > Server.PlayerCount / 2)
                        {
                            player.ShowHint("교도관의 수가 너무 많습니다!", 3);
                        }
                        else
                        {
                            pl.Wander.Add(player.UserId, 0);
                            player.Kill("곧 교도관으로 부활합니다, 준비하세요!");
                        }
                    }
                }

                else if (cz.Contains(pos))
                {
                    if (pl.Prison.Keys.Contains(player.UserId) && pl.Prison[player.UserId] == false)
                    {
                        pl.Prison[player.UserId] = true;
                    }
                }
                else if (pos == "Outside")
                {
                    if (pl.Prison.Keys.Contains(player.UserId))
                    {
                        if (pl.IsOutside.Contains(player.UserId))
                        {

                        }
                        else
                        {
                            player.ShowHint($"교도소에서 탈옥했습니다. 잡히지 마세요!");
                            player.EnableEffect(Exiled.API.Enums.EffectType.Scanned, 6);
                            pl.IsOutside.Add(player.UserId);
                        }
                    }
                }
            }
        }

        void notice(string title, string description)
        {
            TimeSpan timeOfDay = TimeSpan.FromSeconds(St);
            string formattedTime = timeOfDay.ToString("mm\\:ss");
            string amPmDesignator = timeOfDay.Minutes < 12 ? "AM" : "PM";
            string timeset = amPmDesignator + " " + formattedTime;

            if (current != timeset)
            {
                Player.List.ToList().ForEach(x => x.ClearBroadcasts());
                Player.List.ToList().ForEach(x => x.Broadcast(1, $"<size=25><mark=#FF8000aa><color=#000000><b>{title}</b></color></mark></size>\n<size=20><u><mark=#000000aa>{description}</mark></u></size>\n<size=15>[{timeset}]</size>"));
                current = timeset;
            }
        }

        void Update()
        {
            hits.RemoveAll(x => x.player == null);

            if (St < 480)
                St += Time.deltaTime * 10;

            else if ((480 < St && St <= 600) || (840 < St && St <= 960) || (1200 < St && St <= 1380))
                St += Time.deltaTime * 5;

            else if ((600 < St && St <= 840) || (960 < St && St <= 1200))
                St += Time.deltaTime * 3;

            else
                St += Time.deltaTime;

            if (St > 1440) {
                St = 0;
            }

            if (St < 480)
            {
                timestamp = Timestamp.lights_out;
                notice("소등", "모든 수감자는 반드시 각자 방에 있어야 합니다.");
            }
            else if (480 < St && St <= 600)
            {
                timestamp = Timestamp.breakfast;
                notice("아침 식사", "아침 식사 시간입니다. 급식소에서 아침 식사를 제공 받으십시오.");
            }
            else if (600 < St && St <= 840)
            {
                timestamp = Timestamp.yardtime;
                notice("운동 시간", "여러분, 운동 시간입니다. 운동장으로 가세요.");
            }
            else if (840 < St && St <= 960)
            {
                timestamp = Timestamp.lurnch;
                notice("점심 식사", "점심 식사 시간입니다. 전원 식당으로 반드시 출석하세요.");
            }
            else if (960 < St && St <= 1200)
            {
                timestamp = Timestamp.freetime;
                notice("자유 시간", "수감자들을 위한 자유 시간입니다.");
            }
            else if (1200 < St && St <= 1380)
            {
                timestamp = Timestamp.dinner;
                notice("저녁 식사", "모든 수감자는 급식소에서 저녁 식사를 해야 합니다.");
            }
            else if (1380 < St)
            {
                timestamp = Timestamp.lockdown;
                notice("폐방", "수감자는 문을 잠그기 위해 각자 방으로 돌아가야 합니다.");
            }

            if (timestamp != timestamp2)
            {
                timestamp2 = timestamp;
                Prison_Life.Instance.OnTimeChanged(timestamp2);
            }

            foreach (var p in hits)
            {
                try
                {
                    p.Update();
                    Check(p.player);
                }
                catch (Exception ex)
                {

                }
                }
        }

        public Timestamp timestamp = Timestamp.lights_out;
        public Timestamp timestamp2 = Timestamp.lockdown;
        
    }

    public class Prison_Life : Plugin<Config>
    {
        public static Prison_Life Instance;
        public Gtool gtool;

        public List<string> Owner = new List<string>() { "76561198447505804@steam" };
        public List<string> Admin = new List<string>();

        public Dictionary<string, bool> Prison = new Dictionary<string, bool>(); // ID, 범죄 여부
        public Dictionary<string, int> Wander = new Dictionary<string, int>(); // ID, 무고 죄수를 죽인 횟수
        public Dictionary<string, int> Free = new Dictionary<string, int>(); // ID, 범죄 죄수를 죽인 횟수
        public List<string> Ishealing = new List<string>();
        public List<string> IsOutside = new List<string>();
        public List<string> SWAT_PASS = new List<string>() { "76561198447505804@steam", "76561198814547743@steam" };

        public override void OnEnabled()
        {
            Instance = this;

            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStart;

            Exiled.Events.Handlers.Player.Verified += OnVerifed;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Handcuffing += OnHandcuffing;
            Exiled.Events.Handlers.Player.ItemAdded += OnItemAdded;
            Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Player.Jumping += Onjumping;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.SearchingPickup += OnSearchingPickup;
            Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;
            Exiled.Events.Handlers.Player.ReloadingWeapon += OnReloadingWeapon;

            Exiled.Events.Handlers.Item.ChargingJailbird += OnChargingJailbird;

            Exiled.Events.Handlers.Map.PlacingBulletHole += OnPlacingBulletHole;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStart;

            Exiled.Events.Handlers.Player.Verified -= OnVerifed;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Handcuffing -= OnHandcuffing;
            Exiled.Events.Handlers.Player.ItemAdded -= OnItemAdded;
            Exiled.Events.Handlers.Player.FlippingCoin -= OnFlippingCoin;
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            Exiled.Events.Handlers.Player.Jumping -= Onjumping;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;
            Exiled.Events.Handlers.Player.SearchingPickup -= OnSearchingPickup;
            Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;
            Exiled.Events.Handlers.Player.ReloadingWeapon -= OnReloadingWeapon;

            Exiled.Events.Handlers.Item.ChargingJailbird -= OnChargingJailbird;

            Exiled.Events.Handlers.Map.PlacingBulletHole -= OnPlacingBulletHole;

            Instance = null;
        }

        public void OnMelee(Player player)
        {
            if (player.IsDead || player.IsCuffed || player.IsScp)
                return;

            if (Physics.Raycast(player.ReferenceHub.PlayerCameraReference.position + player.ReferenceHub.PlayerCameraReference.forward * 0.2f, player.ReferenceHub.PlayerCameraReference.forward, out RaycastHit hit, 1, InventorySystem.Items.Firearms.Modules.StandardHitregBase.HitregMask) && 
                hit.collider.TryGetComponent<IDestructible>(out IDestructible destructible))
            {
                Hitmarker.SendHitmarkerDirectly(player.ReferenceHub, 1f);
                destructible.Damage(1, new PlayerStatsSystem.CustomReasonDamageHandler("무지성으로 뚜드려 맞아 죽었습니다.", 12), hit.point);
            }
        }

        public void OnTimeChanged(Timestamp NewTimestamp)
        {
            void notice(string note)
            {
                Player.List.Where(x => x.Role.Type != PlayerRoles.RoleTypeId.Tutorial).ToList().ForEach(x => x.ShowHint($"<size=40><mark=#000000aa><b>감옥 방송</b>\n{note}</mark>\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n", 5));
            }

            switch (NewTimestamp)
            {
                case Timestamp.lights_out:
                    notice("모든 수감자는 반드시 각자 방에 있어야 합니다.");
                    break;

                case Timestamp.breakfast:
                    notice("아침 식사 시간입니다. 급식소에서 아침 식사를 제공 받으십시오.");
                    break;

                case Timestamp.yardtime:
                    notice("여러분, 운동 시간입니다. 운동장으로 가세요.");
                    break;

                case Timestamp.lurnch:
                    notice("점심 식사 시간입니다. 전원 식당으로 반드시 출석하세요.");
                    break;

                case Timestamp.freetime:
                    notice("수감자들을 위한 자유 시간입니다.");
                    break;

                case Timestamp.dinner:
                    notice("모든 수감자는 급식소에서 저녁 식사를 해야 합니다.");
                    break;

                case Timestamp.lockdown:
                    notice("수감자는 문을 잠그기 위해 각자 방으로 돌아가야 합니다.");
                    break;

                }
        }

        public void OnWaitingForPlayers()
        {
            Server.ExecuteCommand("/roundlock enable");
            Server.ExecuteCommand("/setconfig friendly_fire true");
            Server.ExecuteCommand("/decontamination disable");
            Server.ExecuteCommand("/forcestart");

        }

        public void OnRoundStart()
        {
            Server.ExecuteCommand("/mp load PL");

            GameObject gameobject = GameObject.Instantiate(new GameObject());
            gtool = gameobject.AddComponent<Gtool>();
        }

        public void OnPlacingBulletHole(Exiled.Events.EventArgs.Map.PlacingBulletHoleEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public async void OnVerifed(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev)
        {
            gtool.hits.Add(new Hit { player = ev.Player });

            if (Wander.Keys.Contains(ev.Player.UserId) || Prison.Keys.Contains(ev.Player.UserId) || Free.Keys.Contains(ev.Player.UserId))
            {
                ev.Player.Kill("곧 재생성됩니다, 준비하세요!");
            }
            else
            {
                ev.Player.Role.Set(PlayerRoles.RoleTypeId.Tutorial);
                ev.Player.EnableEffect(Exiled.API.Enums.EffectType.Invisible);
                Server.ExecuteCommand($"/tp {ev.Player.Id} 141.3819 901.46 -462.6135");
                Server.ExecuteCommand($"/setgroup {ev.Player.Id} mid");

                ev.Player.ShowHint($"<b><size=40><size=50>[<color=#A4A4A4>교도관</color>]</size>\n<mark=#A4A4A4aa>수감자들을 잘 감시하세요. 불법 반입 물품 압수, 폭동 진압, 무엇보다도 탈옥 시도를 저지해야 합니다. 하지만 감옥을 위협하는 것이 죄수뿐만은 아니라는 것, 명심하세요.</mark>\n\n" +
                    $"<size=50>[<color=#FF8000>수감자</color>]</size>\n<mark=#FF8000aa>가석방이 없는 종신형을 받은 무고한 시민인 당신, 어떤 희망도 미래도 보이지 않습니다. 지금 당신은 갈림길에 서있습니다. 평생 추운 감옥에 갇혀 의미 없는 나날을 보낼 것인가, 아니면 탈옥할 것인가...</mark></size></b>\n\n\n" +
                    $"<color=#A4A4A4>교도관</color>으로 플레이하려면 <mark=#0080FFaa><color=#000000>파란색 발판</color></mark>을,\n<color=#FF8000>수감자</color>로 플레이하려면 <mark=#FF8000aa><color=#000000>주황색 발판</color></mark>을 밟으십시오.\n\n\n\n\n\n\n\n\n\n", 10000);

                while (true)
                {
                    if (ev.Player.Role.Type != PlayerRoles.RoleTypeId.Tutorial)
                    {
                        ev.Player.ShowHint("", 1);
                        break;
                    }
                    await Task.Delay(1000);
                }
            }
        }

        public void OnFlippingCoin(Exiled.Events.EventArgs.Player.FlippingCoinEventArgs ev)
        {
            ServerConsole.AddLog($"{ev.Player.Nickname} - {ev.Player.Position.x} {ev.Player.Position.y} {ev.Player.Position.z}", color:ConsoleColor.DarkMagenta);
        }

        public async void OnChangingRole(Exiled.Events.EventArgs.Player.ChangingRoleEventArgs ev)
        {
            Server.ExecuteCommand($"/remotecommand {ev.Player.Id} showtag");

            Player.List.ToList().ForEach(x => x.DisableEffect(Exiled.API.Enums.EffectType.FogControl));
            await Task.Delay(1);
            Player.List.ToList().ForEach(x => x.EnableEffect(Exiled.API.Enums.EffectType.FogControl));
        }

        public void Onjumping(Exiled.Events.EventArgs.Player.JumpingEventArgs ev) 
        {
            if (Physics.Raycast(ev.Player.Position, Vector3.down, out RaycastHit hit, 1, (LayerMask)1))
            {
                if (hit.transform.parent.name == "toilet" && ev.Player.CurrentItem.Type == ItemType.Jailbird)
                {
                    if (UnityEngine.Random.Range(1, 20) == 1)
                    {
                        Server.ExecuteCommand($"/tp {ev.Player.Id} {ev.Player.Position.x} {ev.Player.Position.y - 2} {ev.Player.Position.z}");
                        ev.Player.ShowHint("<b>변기를 성공적으로 부수었습니다!</b>", 3);
                    }
                    else
                    {
                        ev.Player.ShowHint("(무언가 부서지는 소리가..)", 0.3f);
                    }
                }
            }
        }

        public async void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if (ev.Attacker != ev.Player)
            {
                try
                {
                    if (ev.Attacker.CurrentItem.Type == ItemType.Jailbird)
                    {
                        ev.IsAllowed = false;
                        Hitmarker.SendHitmarkerDirectly(ev.Attacker.ReferenceHub, 2f);
                        ev.Player.Hurt(ev.Attacker, 20, Exiled.API.Enums.DamageType.Jailbird, null, "망?치로 뚜드려 맞았습니다.");
                    }

                    else if (ev.Attacker.CurrentItem.Type == ItemType.GunCOM18)
                    {
                        ev.IsAllowed = false;

                        if (!Wander.Keys.Contains(ev.Player.UserId))
                        {
                            ev.Player.EnableEffect(Exiled.API.Enums.EffectType.Ensnared, 1.5f);

                            foreach (var i in System.Linq.Enumerable.Range(1, 30))
                            {
                                ev.Player.CurrentItem = null;
                                await Task.Delay(100);
                            }

                            ev.Player.DisableEffect(Exiled.API.Enums.EffectType.Ensnared);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        public async void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            ev.Player.ClearInventory();

            if (ev.Attacker != null)
            {
                if (Wander.Keys.Contains(ev.Attacker.UserId))
                {
                    if (Wander.Keys.Contains(ev.Player.UserId))
                    {
                        if (Wander[ev.Attacker.UserId] > 1)
                        {
                            Wander.Remove(ev.Attacker.UserId);
                            Prison.Add(ev.Attacker.UserId, false);
                            ev.Attacker.Kill("교도관의 행동 지침을 3번 위반했습니다.");
                        }
                        else
                        {
                            ev.Attacker.ShowHint($"교도관의 행동 지침을 위반했습니다!\n앞으로 {2 - Wander[ev.Attacker.UserId]}번 더 위반하면 당신은 수감됩니다.", 5);
                            Wander[ev.Attacker.UserId] += 1;
                        }
                    }
                    else if (Prison.Keys.Contains(ev.Player.UserId) && !Prison[ev.Player.UserId])
                    {
                        bool IsCrime()
                        {
                            if (Physics.Raycast(ev.Player.Position, Vector3.down, out RaycastHit hit, 1, (LayerMask)1))
                            {
                                Timestamp timestamp2 = gtool.timestamp2;
                                string pos = hit.collider.name;

                                if (timestamp2 == Timestamp.yardtime)
                                {
                                    if (pos != "Playground" && pos != null)
                                    {
                                        if (Prison.Keys.Contains(ev.Player.UserId) && Prison[ev.Player.UserId] == false)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                else if (timestamp2 == Timestamp.dinner || timestamp2 == Timestamp.breakfast || timestamp2 == Timestamp.lurnch)
                                {
                                    if (pos != "Restaurant" && pos != null)
                                    {
                                        if (Prison.Keys.Contains(ev.Player.UserId) && Prison[ev.Player.UserId] == false)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                else if (timestamp2 == Timestamp.lights_out)
                                {
                                    if (pos != "Prison" && pos != null)
                                    {
                                        if (Prison.Keys.Contains(ev.Player.UserId) && Prison[ev.Player.UserId] == false)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            return false;
                        }
                        if (IsCrime())
                        {
                            if (Wander[ev.Attacker.UserId] > 1)
                            {
                                Wander.Remove(ev.Attacker.UserId);
                                Prison.Add(ev.Attacker.UserId, false);
                                ev.Attacker.Kill("교도관의 행동 지침을 3번 위반했습니다.");
                            }
                            else
                            {
                                ev.Attacker.ShowHint($"교도관의 행동 지침을 위반했습니다!\n앞으로 {2 - Wander[ev.Attacker.UserId]}번 더 위반하면 당신은 수감됩니다.", 5);
                                Wander[ev.Attacker.UserId] += 1;
                            }
                        }
                    }
                    else
                    {
                        ev.Attacker.ShowHint($"{ev.Player.Nickname}(을)를 검거하였습니다!", 3);
                    }
                }

                if (Wander.Keys.Contains(ev.Player.UserId))
                {
                    string[] random_items = { "5", "13" };
                    string drop_item = random_items[new System.Random().Next(random_items.Length)];
                    Server.ExecuteCommand($"/drop {ev.Player.Id} {drop_item} 1");
                }

                if (Prison.Keys.Contains(ev.Player.UserId))
                {
                    if (Prison[ev.Player.UserId] = true && Free.Keys.Contains(ev.Attacker.UserId))
                    {
                        if (Free[ev.Attacker.UserId] > 1)
                        {
                            Free.Remove(ev.Attacker.UserId);
                            Wander.Add(ev.Attacker.UserId, 0);
                            ev.Attacker.Kill("용감한 시민상을 받았습니다!");
                        }
                        else
                        {
                            ev.Attacker.ShowHint($"범죄를 저지른 수감자를 처단했습니다!\n앞으로 {2 - Free[ev.Attacker.UserId]}번 더 죽이면 교도관으로 취직합니다.", 5);
                            Free[ev.Attacker.UserId] += 1;
                        }
                    }

                    Prison[ev.Player.UserId] = false;
                    if (IsOutside.Contains(ev.Player.UserId))
                        IsOutside.Remove(ev.Player.UserId);
                }

                if (Free.Keys.Contains(ev.Player.UserId))
                {
                    if (Wander.Keys.Contains(ev.Attacker.UserId))
                    {
                        Free.Remove(ev.Player.UserId);
                        Prison.Add(ev.Player.UserId, false);
                    }
                }
            }

            await Task.Delay(10);
            ev.Player.ClearInventory();
            Server.ExecuteCommand($"/cleanup ragdolls");

            ev.Player.Role.Set(PlayerRoles.RoleTypeId.Tutorial);
            ev.Player.EnableEffect(Exiled.API.Enums.EffectType.Invisible);
            Server.ExecuteCommand($"/tp {ev.Player.Id} 141.3819 901.46 -462.6135");

            for (int i = 1; i < 7; i++)
            {
                ev.Player.ShowHint($"{7 - i}초 후 부활합니다.", 1);
                await Task.Delay(1000);
            }

            if (Prison.Keys.Contains(ev.Player.UserId))
            {
                BornPrison(ev);
            }
            else if (Wander.Keys.Contains(ev.Player.UserId))
            {
                BornWander(ev);
            }
            else if (Free.Keys.Contains(ev.Player.UserId))
            {
                BornFree(ev);
            }
        }

        public async void OnHandcuffing(Exiled.Events.EventArgs.Player.HandcuffingEventArgs ev)
        {
            if (Wander.Keys.Contains(ev.Player.UserId))
            {
                if (!Prison[ev.Target.UserId])
                {
                    if (Wander[ev.Player.UserId] > 1)
                    {
                        Wander.Remove(ev.Player.UserId);
                        Prison.Add(ev.Player.UserId, false);
                        ev.Player.Kill("교도관의 행동 지침을 3번 위반했습니다.");
                    }
                    else
                    {
                        ev.Player.ShowHint($"교도관의 행동 지침을 위반했습니다!\n앞으로 {2 - Wander[ev.Player.UserId]}번 더 위반하면 당신은 수감됩니다.", 5);
                        Wander[ev.Player.UserId] += 1;
                    }
                }
                ev.Player.ShowHint($"{ev.Target.Nickname}(을)를 검거하였습니다!");

                if (Prison.Keys.Contains(ev.Target.UserId) && UnityEngine.Random.Range(1, 5) == 1 && Wander.Keys.Count() < Server.PlayerCount / 2)
                {
                    Prison.Remove(ev.Target.UserId);
                    Wander.Add(ev.Target.UserId, 0);
                    ev.Target.Kill("징집당했습니다.");
                }
                else if (Prison.Keys.Contains(ev.Target.UserId) || Free.Keys.Contains(ev.Target.UserId))
                {
                    if (Free.Keys.Contains(ev.Target.UserId))
                    {
                        Free.Remove(ev.Target.UserId);
                        Prison.Add(ev.Target.UserId, false);
                    }

                    ev.Target.EnableEffect(Exiled.API.Enums.EffectType.Ensnared);
                    for (int i = 1; i < 5; i++)
                    {
                        ev.Target.ShowHint($"{5 - i}초 후 투옥됩니다.", 1);
                        await Task.Delay(1000);
                    }
                    ev.Target.Kill("교도관에 의해 체포당했습니다.");
                }
            }
        }

        public void OnReloadingWeapon(Exiled.Events.EventArgs.Player.ReloadingWeaponEventArgs ev)
        {
            if (ev.Item.Type == ItemType.GunCOM18)
            {
                if (ev.Firearm.Ammo > 0)
                {
                    ev.IsAllowed = false;
                }
                else
                {
                    ev.Firearm.MaxAmmo = 1;
                }
            }
        }

        public void OnChangingItem(Exiled.Events.EventArgs.Player.ChangingItemEventArgs ev)
        {
            try
            {
                if (ev.Item.Type == ItemType.GunCOM18)
                {
                    if (ev.Item.As<Exiled.API.Features.Items.Firearm>().Ammo > 1)
                    {
                        ev.Item.As<Exiled.API.Features.Items.Firearm>().Ammo = 1;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void OnItemAdded(Exiled.Events.EventArgs.Player.ItemAddedEventArgs ev)
        {
            if (Prison.Keys.Contains(ev.Player.UserId) && !Prison[ev.Player.UserId])
            {
                Prison[ev.Player.UserId] = true;
            }
        }

        public void OnChargingJailbird(Exiled.Events.EventArgs.Item.ChargingJailbirdEventArgs ev)
        {
            ev.IsAllowed = false;

            ev.Player.ShowHint("제일버드의 저주..");
        }

        public void OnSearchingPickup(Exiled.Events.EventArgs.Player.SearchingPickupEventArgs ev)
        {
            ItemType[] Blacks = { ItemType.KeycardMTFCaptain };
            ItemType[] Ammos = { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 };
            ItemType[] SItems = { ItemType.GunE11SR };
            ItemType[] Armors = { ItemType.ArmorLight, ItemType.ArmorCombat, ItemType.ArmorHeavy };

            if (Blacks.Contains(ev.Pickup.Type))
            {
                ev.IsAllowed = false;
                return;
            }

            if (ev.Player.HasItem(ev.Pickup.Type) && !Ammos.Contains(ev.Pickup.Type))
            {
                ev.IsAllowed = false;
                return;
            }

            foreach (var Armor in Armors)
            {
                if (ev.Player.HasItem(Armor) && Armors.Contains(ev.Pickup.Type))
                {
                    ev.IsAllowed = false;
                    return;
                }
            }
            foreach (var S in SItems)
            {
                if (ev.Player.HasItem(S) && ev.Pickup.Type == S)
                {
                    ev.IsAllowed = false;
                    return;
                }
            }

            if (ev.Pickup.Type == ItemType.GunE11SR || ev.Pickup.Type == ItemType.Ammo556x45)
            {
                if (SWAT_PASS.Contains(ev.Player.UserId))
                {
                    ev.Player.AddItem(ev.Pickup.Type);
                }
            }
            else if (ev.Pickup.Type == ItemType.ArmorHeavy)
            {
                if (SWAT_PASS.Contains(ev.Player.UserId) && Wander.Keys.Contains(ev.Player.UserId))
                {
                    ev.Player.Role.Set(PlayerRoles.RoleTypeId.NtfPrivate, Exiled.API.Enums.SpawnReason.ForceClass, 0);
                    Server.ExecuteCommand($"/setgroup {ev.Player.Id} swat");
                    ev.Player.AddItem(ev.Pickup.Type);
                }
            }
            else
            {
                ev.Player.AddItem(ev.Pickup.Type);
            }
        }

        public void OnDroppingItem(Exiled.Events.EventArgs.Player.DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public void OnDroppingAmmo(Exiled.Events.EventArgs.Player.DroppingAmmoEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public async void BornPrison(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            ev.Player.Role.Set(PlayerRoles.RoleTypeId.ClassD);
            ev.Player.IsGodModeEnabled = true;
            
            string[] prison_spots = { "63.16169 902.2742 -441.3847", "63.08948 902.2742 -436.1318", "62.97985 902.2742 -431.0825", "63.05719 902.2742 -425.9802",
                                    "94.96423 902.2742 -423.5239", "95.23376 902.2742 -428.3442", "95.28063 902.2742 -433.3325", "95.42932 902.2742 -438.4169",
                                    "62.99055 907.3542 -441.1018", "63.04651 907.3516 -436.2646", "62.97204 907.3541 -431.2582", "62.82751 907.3541 -426.2934",
                                    "95.06579 907.3541 -423.2504", "95.10094 907.3541 -428.3637", "94.98682 907.3541 -433.2708", "95.10011 907.3541 -438.4184" };
            string prison = prison_spots[new System.Random().Next(prison_spots.Length)];
            Server.ExecuteCommand($"/tp {ev.Player.Id} {prison}");

            Server.ExecuteCommand($"/bypass {ev.Player.Id} 0");
            Server.ExecuteCommand($"/size {ev.Player.Id} 1 1 1");
            Server.ExecuteCommand($"/setgroup {ev.Player.Id} prison");

            await Task.Delay(7000);

            ev.Player.IsGodModeEnabled = false;
        }

        public async void BornWander(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            ev.Player.Role.Set(PlayerRoles.RoleTypeId.FacilityGuard);
            ev.Player.ClearInventory();

            ev.Player.AddItem(ItemType.GunCOM18);

            ev.Player.AddItem(ItemType.GunCOM15);
            ev.Player.AddItem(ItemType.Ammo9x19);
            ev.Player.IsGodModeEnabled = true;

            string[] wander_spots = { "99.87569 901.46 -479.4201", "99.79726 901.46 -482.4286", "99.74648 901.46 -485.5341" };
            string wander = wander_spots[new System.Random().Next(wander_spots.Length)];
            Server.ExecuteCommand($"/tp {ev.Player.Id} {wander}");
            Server.ExecuteCommand($"/bypass {ev.Player.Id} 1");
            Server.ExecuteCommand($"/size {ev.Player.Id} 1 1 1");
            Server.ExecuteCommand($"/setgroup {ev.Player.Id} warden");

            await Task.Delay(7000);

            ev.Player.IsGodModeEnabled = false;
        }

        public async void BornFree(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            if (UnityEngine.Random.Range(1, 50) == 1)
            {
                ev.Player.Role.Set(PlayerRoles.RoleTypeId.ChaosRepressor);
                ev.Player.ClearInventory();
                ev.Player.IsGodModeEnabled = true;
                ev.Player.AddItem(ItemType.KeycardChaosInsurgency);
                ev.Player.AddItem(ItemType.GunLogicer);
                ev.Player.AddItem(ItemType.Ammo762x39, 10);
                ev.Player.EnableEffect(Exiled.API.Enums.EffectType.SinkHole);
                ev.Player.Health = 300;

                string[] free_spots = { "497.0742 900.5234 -518.8945", "497.1016 900.3984 -516.9297", "496.9766 899.9023 -514.3867",
                                     "511.518 903.596 -547.4103", "511.5352 903.596 -544.1328", "511.6289 903.596 -540.957" };
                string free = free_spots[new System.Random().Next(free_spots.Length)];
                Server.ExecuteCommand($"/tp {ev.Player.Id} {free}");
                Server.ExecuteCommand($"/bypass {ev.Player.Id} 0");
                Server.ExecuteCommand($"/size {ev.Player.Id} 1.2 1.1 1.2");
                Server.ExecuteCommand($"/setgroup {ev.Player.Id} juggernaut");

                await Task.Delay(7000);

                ev.Player.IsGodModeEnabled = false;
            }
            else
            {
                ev.Player.Role.Set(PlayerRoles.RoleTypeId.Tutorial);
                ev.Player.ClearInventory();
                ev.Player.IsGodModeEnabled = true;

                string[] free_spots = { "497.0742 900.5234 -518.8945", "497.1016 900.3984 -516.9297", "496.9766 899.9023 -514.3867",
                                     "511.518 903.596 -547.4103", "511.5352 903.596 -544.1328", "511.6289 903.596 -540.957" };
                string free = free_spots[new System.Random().Next(free_spots.Length)];
                Server.ExecuteCommand($"/tp {ev.Player.Id} {free}");
                Server.ExecuteCommand($"/bypass {ev.Player.Id} 0");
                Server.ExecuteCommand($"/size {ev.Player.Id} 1 1 1");
                Server.ExecuteCommand($"/setgroup {ev.Player.Id} free");

                await Task.Delay(7000);

                ev.Player.IsGodModeEnabled = false;
            }
        }
    }
}
