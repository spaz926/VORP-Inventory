﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using VorpInventory.Database;
using VorpInventory.Diagnostics;
using VorpInventory.Models;

namespace VorpInventory.Scripts
{
    public class VorpCoreInvenoryAPI : BaseScript
    {
        public static Dictionary<string, CallbackDelegate> usableItemsFunctions = new Dictionary<string, CallbackDelegate>();

        PlayerList PlayerList => PluginManager.PlayerList;

        internal VorpCoreInvenoryAPI()
        {
            EventHandlers["vorpCore:subWeapon"] += new Action<int, int>(subWeapon);
            EventHandlers["vorpCore:giveWeapon"] += new Action<int, int, int>(giveWeapon);
            EventHandlers["vorpCore:registerWeapon"] += new Action<int, string, ExpandoObject, ExpandoObject>(registerWeapon);
            EventHandlers["vorpCore:addItem"] += new Action<int, string, int>(addItem);
            EventHandlers["vorpCore:subItem"] += new Action<int, string, int>(SubtractItem);
            EventHandlers["vorpCore:getItemCount"] += new Action<int, CallbackDelegate, string>(getItems);
            EventHandlers["vorpCore:getUserInventory"] += new Action<int, CallbackDelegate>(getInventory);
            EventHandlers["vorpCore:canCarryItems"] += new Action<int, int, CallbackDelegate>(canCarryAmountItem);
            EventHandlers["vorpCore:canCarryItem"] += new Action<int, string, int, CallbackDelegate>(canCarryItem);
            EventHandlers["vorpCore:canCarryWeapons"] += new Action<int, int, CallbackDelegate>(canCarryAmountWeapons);
            EventHandlers["vorpCore:subBullets"] += new Action<int, int, string, int>(subBullets);
            EventHandlers["vorpCore:addBullets"] += new Action<int, int, string, int>(addBullets);
            EventHandlers["vorpCore:getWeaponComponents"] += new Action<int, CallbackDelegate, int>(getWeaponComponents);
            EventHandlers["vorpCore:getWeaponBullets"] += new Action<int, CallbackDelegate, int>(getWeaponBullets);
            EventHandlers["vorpCore:getUserWeapons"] += new Action<int, CallbackDelegate>(getUserWeapons);
            EventHandlers["vorpCore:addComponent"] += new Action<int, int, string, CallbackDelegate>(addComponent);
            EventHandlers["vorpCore:getUserWeapon"] += new Action<int, CallbackDelegate, int>(getUserWeapon);
            EventHandlers["vorpCore:registerUsableItem"] += new Action<string, CallbackDelegate>(registerUsableItem);
            EventHandlers["vorp:use"] += new Action<Player, string, object[]>(useItem);
        }

        public async Task SaveInventoryItemsSupport(Player source)
        {
            await Delay(1000);
            string identifier = "steam:" + source.Identifiers["steam"];
            Dictionary<string, int> items = new Dictionary<string, int>();
            if (ItemDatabase.UserInventory.ContainsKey(identifier))
            {
                foreach (var item in ItemDatabase.UserInventory[identifier])
                {
                    items.Add(item.Key, item.Value.getCount());
                }
                if (items.Count >= 0)
                {
                    dynamic CoreUser = PluginManager.CORE.getUser(int.Parse(source.Handle)).getUsedCharacter;
                    int charIdentifier = CoreUser.charIdentifier;
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(items);
                    Exports["ghmattimysql"].execute($"UPDATE characters SET inventory = '{json}' WHERE `identifier` = ? AND `charidentifier` = ?;", new object[] { identifier, charIdentifier });
                }
            }
        }

        private void canCarryAmountWeapons(int source, int quantity, CallbackDelegate cb)
        {
            Player p = PlayerList[source];

            if (p == null)
            {
                Logger.Error($"canCarryAmountWeapons: Player '{source}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];
            dynamic CoreUser = PluginManager.CORE.getUser(source).getUsedCharacter;
            int charIdentifier = CoreUser.charIdentifier;
            int totalcount = getUserTotalCountWeapons(identifier, charIdentifier) + quantity;
            if (Config.MaxWeapons != -1)
            {
                if (totalcount <= Config.MaxWeapons)
                {
                    cb.Invoke(true);
                }
                else
                {
                    cb.Invoke(false);
                }
            }
            else
            {
                cb.Invoke(true);
            }

        }

        private void canCarryAmountItem(int source, int quantity, CallbackDelegate cb)
        {
            Player p = PlayerList[source];

            if (p == null)
            {
                Logger.Error($"canCarryAmountItem: Player '{source}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];
            if (ItemDatabase.UserInventory.ContainsKey(identifier) && Config.MaxItems != -1)
            {
                int totalcount = getUserTotalCount(identifier) + quantity;
                if ((totalcount <= Config.MaxItems))
                {
                    cb.Invoke(true);
                }
                else
                {
                    cb.Invoke(false);
                }
            }
            else
            {
                cb.Invoke(true);
            }

        }

        private void canCarryItem(int source, string itemName, int quantity, CallbackDelegate cb)
        {
            Player p = PlayerList[source];

            if (p == null)
            {
                Logger.Error($"canCarryItem: Player '{source}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];


            if (ItemDatabase.ServerItems.ContainsKey(itemName))
            {
                int limit = ItemDatabase.ServerItems[itemName].getLimit();


                if (limit != -1)
                {
                    if (ItemDatabase.UserInventory.ContainsKey(identifier))
                    {
                        if (ItemDatabase.UserInventory[identifier].ContainsKey(itemName))
                        {
                            int count = ItemDatabase.UserInventory[identifier][itemName].getCount();

                            int total = count + quantity;

                            if (total <= limit)
                            {
                                if (Config.MaxItems != -1)
                                {
                                    int totalcount = getUserTotalCount(identifier) + quantity;
                                    if ((totalcount <= Config.MaxItems))
                                    {
                                        cb.Invoke(true);
                                    }
                                    else
                                    {
                                        cb.Invoke(false);
                                    }
                                }
                                else
                                {
                                    cb.Invoke(true);
                                }
                            }
                            else
                            {
                                cb.Invoke(false);
                            }

                        }
                        else
                        {
                            if (quantity <= limit)
                            {
                                if (Config.MaxItems != -1)
                                {
                                    int totalcount = getUserTotalCount(identifier) + quantity;
                                    if ((totalcount <= Config.MaxItems))
                                    {
                                        cb.Invoke(true);
                                    }
                                    else
                                    {
                                        cb.Invoke(false);
                                    }
                                }
                                else
                                {
                                    cb.Invoke(true);
                                }
                            }
                            else
                            {
                                cb.Invoke(false);
                            }
                        }
                    }
                    else
                    {
                        if (quantity <= limit)
                        {
                            if (Config.MaxItems != -1)
                            {
                                int totalcount = quantity;
                                if ((totalcount <= Config.MaxItems))
                                {
                                    cb.Invoke(true);
                                }
                                else
                                {
                                    cb.Invoke(false);
                                }
                            }
                            else
                            {
                                cb.Invoke(true);
                            }
                        }
                        else
                        {
                            cb.Invoke(false);
                        }
                    }

                }
                else
                {
                    if (Config.MaxItems != -1)
                    {
                        int totalcount = getUserTotalCount(identifier) + quantity;
                        if ((totalcount <= Config.MaxItems))
                        {
                            cb.Invoke(true);
                        }
                        else
                        {
                            cb.Invoke(false);
                        }
                    }
                    else
                    {
                        cb.Invoke(true);
                    }
                }
            }
            else
            {
                Debug.WriteLine($"Use Item Error{itemName}");
            }


        }

        private void getInventory(int source, CallbackDelegate cb)
        {
            try
            {
                Player player = PlayerList[source];

                if (player == null)
                {
                    Logger.Error($"getInventory: Player doesn't exist, but why?!.");
                    return;
                }

                string identifier = "steam:" + player.Identifiers["steam"];
                if (ItemDatabase.UserInventory.ContainsKey(identifier))
                {
                    List<object> useritems = new List<object>();
                    var itemsDBO = ItemDatabase.UserInventory[identifier];

                    if (itemsDBO == null)
                    {
                        Logger.Error($"getInventory: Player '{player.Name}' has no items.");
                        cb.Invoke(useritems);
                    }

                    foreach (var items in itemsDBO)
                    {
                        Dictionary<string, object> item = new Dictionary<string, object>()
                        {
                            {"label", items.Value.getLabel()},
                            {"name", items.Value.getName()},
                            {"type", items.Value.getType()},
                            {"count", items.Value.getCount()},
                            {"limit", items.Value.getLimit()},
                            {"usable", items.Value.getUsable()}
                        };

                        useritems.Add(item);
                    }

                    cb.Invoke(useritems);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "getInventory");
            }
        }

        private void useItem([FromSource] Player source, string itemName, params object[] args)
        {
            try
            {
                if (!usableItemsFunctions.ContainsKey(itemName))
                {
                    Logger.Error($"Item '{itemName}' doesn't exist as a usable item");
                    return;
                }

                Items item = ItemDatabase.GetItem(itemName);
                if (item == null)
                {
                    Logger.Error($"Item '{itemName}' not found in Server Items.");
                    return;
                }

                Dictionary<string, object> argumentos = new()
                    {
                        {"source", int.Parse(source.Handle)},
                        {"item", ItemDatabase.ServerItems[itemName].getItemDictionary()},
                        {"args",args}
                    };
                usableItemsFunctions[itemName](argumentos);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"useItem");
            }
        }

        private void registerUsableItem(string name, CallbackDelegate cb)
        {
            usableItemsFunctions[name] = cb;
            Logger.Info($"{API.GetCurrentResourceName()}: Function callback of usable item: {name} registered!");
        }

        private void subComponent(int player, int weaponId, string component, CallbackDelegate function)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"subComponent: Player '{player}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];

            if (ItemDatabase.UserWeapons.ContainsKey(weaponId))
            {
                if (ItemDatabase.UserWeapons[weaponId].getPropietary() == identifier)
                {
                    ItemDatabase.UserWeapons[weaponId].quitComponent(component);
                    Exports["ghmattimysql"]
                        .execute(
                            $"UPDATE loadout SET components = '{Newtonsoft.Json.JsonConvert.SerializeObject(ItemDatabase.UserWeapons[weaponId].getAllComponents())}' WHERE id=?",
                            new[] { weaponId });
                    function.Invoke(true);
                    p.TriggerEvent("vorpCoreClient:subComponent", weaponId, component);
                }
                else
                {
                    function.Invoke(false);
                }
            }
        }

        private void addComponent(int player, int weaponId, string component, CallbackDelegate function)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"addComponent: Player '{player}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];

            if (ItemDatabase.UserWeapons.ContainsKey(weaponId))
            {
                if (ItemDatabase.UserWeapons[weaponId].getPropietary() == identifier)
                {
                    ItemDatabase.UserWeapons[weaponId].setComponent(component);

                    Exports["ghmattimysql"]
                        .execute(
                            $"UPDATE loadout SET components = '{Newtonsoft.Json.JsonConvert.SerializeObject(ItemDatabase.UserWeapons[weaponId].getAllComponents())}' WHERE id=?",
                            new[] { weaponId });
                    function.Invoke(true);
                    p.TriggerEvent("vorpCoreClient:addComponent", weaponId, component);
                }
                else
                {
                    function.Invoke(false);
                }
            }
        }

        private void getUserWeapon(int player, CallbackDelegate function, int weapId)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"getUserWeapon: Player '{player}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];

            Dictionary<string, dynamic> weapons = new Dictionary<string, dynamic>();
            bool found = false;
            foreach (KeyValuePair<int, WeaponClass> weapon in ItemDatabase.UserWeapons)
            {
                if (weapon.Value.getId() == weapId && !found)
                {
                    weapons.Add("name", weapon.Value.getName());
                    weapons.Add("id", weapon.Value.getId());
                    weapons.Add("propietary", weapon.Value.getPropietary());
                    weapons.Add("used", weapon.Value.getUsed());
                    weapons.Add("ammo", weapon.Value.getAllAmmo());
                    weapons.Add("components", weapon.Value.getAllComponents());
                    found = true;
                }
            }
            function.Invoke(weapons);
        }

        private void getUserWeapons(int player, CallbackDelegate function)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"getUserWeapons: Player '{player}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];
            int charIdentifier;
            try
            {
                dynamic CoreUser = PluginManager.CORE.getUser(player).getUsedCharacter;
                charIdentifier = CoreUser.charIdentifier;
            }
            catch
            {
                charIdentifier = -1;
            }

            Dictionary<string, dynamic> weapons;
            List<Dictionary<string, dynamic>> userWeapons = new List<Dictionary<string, dynamic>>();

            foreach (KeyValuePair<int, WeaponClass> weapon in ItemDatabase.UserWeapons)
            {
                if (weapon.Value.getPropietary() == identifier)
                {

                    if (weapon.Value.getCharId() == charIdentifier)
                    {
                        weapons = new Dictionary<string, dynamic>
                        {
                            ["name"] = weapon.Value.getName(),
                            ["id"] = weapon.Value.getId(),
                            ["propietary"] = weapon.Value.getPropietary(),
                            ["used"] = weapon.Value.getUsed(),
                            ["ammo"] = weapon.Value.getAllAmmo(),
                            ["components"] = weapon.Value.getAllComponents()
                        };
                        userWeapons.Add(weapons);
                    }
                }
            }
            function.Invoke(userWeapons);
        }

        private void getWeaponBullets(int player, CallbackDelegate function, int weaponId)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"getWeaponBullets: Player '{player}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];

            if (ItemDatabase.UserWeapons.ContainsKey(weaponId))
            {
                if (ItemDatabase.UserWeapons[weaponId].getPropietary() == identifier)
                {
                    function.Invoke(ItemDatabase.UserWeapons[weaponId].getAllAmmo());
                }
            }
        }

        private void getWeaponComponents(int player, CallbackDelegate function, int weaponId)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"getWeaponComponents: Player '{player}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];

            if (ItemDatabase.UserWeapons.ContainsKey(weaponId))
            {
                if (ItemDatabase.UserWeapons[weaponId].getPropietary() == identifier)
                {
                    function.Invoke(ItemDatabase.UserWeapons[weaponId].getAllComponents());
                }
            }
        }

        private void addBullets(int player, int weaponId, string bulletType, int cuantity)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"addBullets: Player '{player}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];

            if (ItemDatabase.UserWeapons.ContainsKey(weaponId))
            {
                if (ItemDatabase.UserWeapons[weaponId].getPropietary() == identifier)
                {
                    ItemDatabase.UserWeapons[weaponId].addAmmo(cuantity, bulletType);
                    p.TriggerEvent("vorpCoreClient:addBullets", weaponId, bulletType, cuantity);
                }
            }
            else
            {
                Debug.WriteLine("Weapon not found in DBa");
            }
        }

        private void subBullets(int player, int weaponId, string bulletType, int cuantity)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"subBullets: Player '{player}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];

            if (ItemDatabase.UserWeapons.ContainsKey(weaponId))
            {
                if (ItemDatabase.UserWeapons[weaponId].getPropietary() == identifier)
                {
                    ItemDatabase.UserWeapons[weaponId].subAmmo(cuantity, bulletType);
                    p.TriggerEvent("vorpCoreClient:subBullets", weaponId, bulletType, cuantity);
                }
            }
            else
            {
                Debug.WriteLine("Weapon not found in DB");
            }
        }

        private void getItems(int source, CallbackDelegate funcion, string item)
        {
            Player p = PlayerList[source];

            if (p == null)
            {
                Logger.Error($"getItems: Player '{source}' does not exist.");
                return;
            }

            string identifier = "steam:" + p.Identifiers["steam"];
            if (ItemDatabase.UserInventory.ContainsKey(identifier))
            {
                if (ItemDatabase.UserInventory[identifier].ContainsKey(item))
                {
                    funcion.Invoke(ItemDatabase.UserInventory[identifier][item].getCount());
                }
                else
                {
                    funcion.Invoke(0);
                }
            }
        }
        private async void addItem(int player, string name, int cuantity)
        {
            try
            {
                if (!ItemDatabase.ServerItems.ContainsKey(name))
                {
                    Debug.WriteLine($"addItem: Item: {name} not exist on Database please add this item on Table `Items`");
                    return;
                }

                Player p = PlayerList[player];

                if (p == null)
                {
                    Logger.Error($"addItem: Player '{player}' does not exist.");
                    return;
                }

                bool added = false;
                string identifier = "steam:" + p.Identifiers["steam"];

                if (!ItemDatabase.UserInventory.ContainsKey(identifier))
                {
                    Dictionary<string, ItemClass> userinv = new Dictionary<string, ItemClass>();
                    ItemDatabase.UserInventory.Add(identifier, userinv);
                }

                if (ItemDatabase.UserInventory.ContainsKey(identifier))
                {
                    if (ItemDatabase.UserInventory[identifier].ContainsKey(name))
                    {
                        if (ItemDatabase.UserInventory[identifier][name].getCount() + cuantity <= ItemDatabase.UserInventory[identifier][name].getLimit())
                        {
                            if (cuantity > 0)
                            {
                                if (Config.MaxItems != 0)
                                {
                                    int totalcount = getUserTotalCount(identifier);
                                    totalcount += cuantity;
                                    if (totalcount <= Config.MaxItems)
                                    {
                                        added = true;
                                        ItemDatabase.UserInventory[identifier][name].addCount(cuantity);
                                    }
                                }
                                else
                                {
                                    added = true;
                                    ItemDatabase.UserInventory[identifier][name].addCount(cuantity);
                                }
                            }
                        }
                        else if (ItemDatabase.UserInventory[identifier][name].getLimit() == -1)
                        {
                            if (cuantity > 0)
                            {
                                if (Config.MaxItems != 0)
                                {
                                    int totalcount = getUserTotalCount(identifier);
                                    totalcount += cuantity;
                                    if (totalcount <= Config.MaxItems)
                                    {
                                        added = true;
                                        ItemDatabase.UserInventory[identifier][name].addCount(cuantity);
                                    }
                                }
                                else
                                {
                                    added = true;
                                    ItemDatabase.UserInventory[identifier][name].addCount(cuantity);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (cuantity <= ItemDatabase.ServerItems[name].getLimit())
                        {
                            added = true;

                            if (Config.MaxItems != 0)
                            {
                                int totalcount = getUserTotalCount(identifier);
                                totalcount += cuantity;
                                if (totalcount <= Config.MaxItems)
                                {
                                    added = true;
                                    ItemDatabase.UserInventory[identifier].Add(name, new ItemClass(cuantity, ItemDatabase.ServerItems[name].getLimit(),
                                ItemDatabase.ServerItems[name].getLabel(), name, ItemDatabase.ServerItems[name].getType(), true, ItemDatabase.ServerItems[name].getCanRemove()));
                                }
                            }
                            else
                            {
                                added = true;
                                ItemDatabase.UserInventory[identifier].Add(name, new ItemClass(cuantity, ItemDatabase.ServerItems[name].getLimit(),
                                ItemDatabase.ServerItems[name].getLabel(), name, ItemDatabase.ServerItems[name].getType(), true, ItemDatabase.ServerItems[name].getCanRemove()));
                            }


                        }
                        else if (ItemDatabase.ServerItems[name].getLimit() == -1)
                        {
                            if (Config.MaxItems != 0)
                            {
                                int totalcount = getUserTotalCount(identifier);
                                totalcount += cuantity;
                                if (totalcount <= Config.MaxItems)
                                {
                                    added = true;
                                    ItemDatabase.UserInventory[identifier].Add(name, new ItemClass(cuantity, ItemDatabase.ServerItems[name].getLimit(),
                                        ItemDatabase.ServerItems[name].getLabel(), name, ItemDatabase.ServerItems[name].getType(), true, ItemDatabase.ServerItems[name].getCanRemove()));
                                }
                            }
                            else
                            {
                                added = true;
                                ItemDatabase.UserInventory[identifier].Add(name, new ItemClass(cuantity, ItemDatabase.ServerItems[name].getLimit(),
                                    ItemDatabase.ServerItems[name].getLabel(), name, ItemDatabase.ServerItems[name].getType(), true, ItemDatabase.ServerItems[name].getCanRemove()));
                            }

                        }

                    }
                    if (ItemDatabase.UserInventory[identifier].ContainsKey(name) && added)
                    {
                        int limit = ItemDatabase.UserInventory[identifier][name].getLimit();
                        string label = ItemDatabase.UserInventory[identifier][name].getLabel();
                        string type = ItemDatabase.UserInventory[identifier][name].getType();
                        bool usable = ItemDatabase.UserInventory[identifier][name].getUsable();
                        bool canRemove = ItemDatabase.UserInventory[identifier][name].getCanRemove();
                        p.TriggerEvent("vorpCoreClient:addItem", cuantity, limit, label, name, type, usable, canRemove);//Pass item to client
                        SaveInventoryItemsSupport(p);
                    }
                    else
                    {
                        TriggerClientEvent(p, "vorp:Tip", Config.lang["fullInventory"], 2000);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void SubtractItem(int player, string name, int quantity)
        {
            try
            {
                if (ItemDatabase.GetItem(name) == null)
                {
                    Debug.WriteLine($"Item: {name} not exist on Database please add this item on Table `Items`");
                    return;
                }

                Player p = PlayerList[player];

                if (p == null)
                {
                    Logger.Error($"subItem: Player '{player}' does not exist.");
                    return;
                }

                string identifier = "steam:" + p.Identifiers["steam"];

                Dictionary<string, ItemClass> userInventory = ItemDatabase.GetInventory(identifier);
                if (userInventory == null)
                {
                    Logger.Error($"subItem: Player '{player}' inventory does not exist.");
                    return;
                }

                if (userInventory.ContainsKey(name))
                {
                    ItemClass item = userInventory[name];
                    int itemCount = item.getCount();

                    if (quantity <= itemCount)
                    {
                        itemCount = item.Subtract(quantity);
                    }

                    if (itemCount == 0)
                    {
                        userInventory.Remove(name);
                    }

                    p.TriggerEvent("vorpCoreClient:subItem", name, itemCount);
                    SaveInventoryItemsSupport(p);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"VORP Inventory: subItem");
            }
        }

        private void registerWeapon(int target, string name, ExpandoObject ammos, ExpandoObject components)//Needs dirt level
        {
            Player p = null;
            bool targetIsPlayer = false;
            foreach (Player pla in PlayerList)
            {
                if (int.Parse(pla.Handle) == target)
                {
                    p = PlayerList[target];
                    targetIsPlayer = true;
                }
            }

            if (p == null)
            {
                Logger.Error($"registerWeapon: Target Player '{target}' does not exist.");
                return;
            }

            string identifier;
            dynamic CoreUser = PluginManager.CORE.getUser(target).getUsedCharacter;
            int charIdentifier = CoreUser.charIdentifier;

            if (targetIsPlayer)
            {
                identifier = "steam:" + p.Identifiers["steam"];
                if (Config.MaxWeapons != 0)
                {
                    int totalcount = getUserTotalCountWeapons(identifier, charIdentifier);
                    totalcount += 1;
                    if (totalcount > Config.MaxWeapons)
                    {
                        Debug.WriteLine($"{p.Name} Can't carry more weapons");
                        return;
                    }
                }
            }
            else
            {
                identifier = target.ToString();
            }

            Dictionary<string, int> ammoaux = new Dictionary<string, int>();
            if (ammos != null)
            {
                foreach (KeyValuePair<string, object> ammo in ammos)
                {
                    ammoaux.Add(ammo.Key, int.Parse(ammo.Value.ToString()));
                }
            }

            List<string> auxcomponents = new List<string>();
            if (components != null)
            {
                foreach (KeyValuePair<string, object> component in components)
                {
                    auxcomponents.Add(component.Key);
                }
            }

            Exports["ghmattimysql"].execute("INSERT INTO loadout (`identifier`,`charidentifier`,`name`,`ammo`,`components`) VALUES (?,?,?,?,?)", new object[] { identifier, charIdentifier, name, Newtonsoft.Json.JsonConvert.SerializeObject(ammoaux), Newtonsoft.Json.JsonConvert.SerializeObject(auxcomponents) }, new Action<dynamic>((result) =>
            {
                int weaponId = result.insertId;
                WeaponClass auxWeapon = new WeaponClass(weaponId, identifier, name, ammoaux, auxcomponents, false, false, charIdentifier);
                ItemDatabase.UserWeapons.Add(weaponId, auxWeapon);
                if (targetIsPlayer)
                {
                    TriggerEvent("syn_weapons:registerWeapon", weaponId);
                    p.TriggerEvent("vorpinventory:receiveWeapon", weaponId, ItemDatabase.UserWeapons[weaponId].getPropietary(),
                        ItemDatabase.UserWeapons[weaponId].getName(), ItemDatabase.UserWeapons[weaponId].getAllAmmo(), ItemDatabase.UserWeapons[weaponId].getAllComponents());
                }
            }));
        }

        private void giveWeapon(int player, int weapId, int target)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"giveWeapon: Player '{player}' does not exist.");
                return;
            }

            Player ptarget = null;
            bool targetIsPlayer = false;
            foreach (Player pla in PlayerList)
            {
                if (int.Parse(pla.Handle) == target)
                {
                    targetIsPlayer = true;
                }
            }

            if (targetIsPlayer)
            {
                ptarget = PlayerList[target];

                if (ptarget == null)
                {
                    Logger.Error($"giveWeapon: Target Player '{target}' does not exist.");
                    return;
                }
            }

            string identifier = "steam:" + p.Identifiers["steam"];
            dynamic CoreUser = PluginManager.CORE.getUser(player).getUsedCharacter;
            int charIdentifier = CoreUser.charIdentifier;

            if (Config.MaxWeapons != 0)
            {
                int totalcount = getUserTotalCountWeapons(identifier, charIdentifier);
                totalcount += 1;
                if (totalcount > Config.MaxWeapons)
                {
                    Debug.WriteLine($"{p.Name} Can't carry more weapons");
                    return;
                }
            }

            if (ItemDatabase.UserWeapons.ContainsKey(weapId))
            {
                ItemDatabase.UserWeapons[weapId].setPropietary(identifier);
                ItemDatabase.UserWeapons[weapId].setCharId(charIdentifier);
                Exports["ghmattimysql"]
                    .execute(
                        $"UPDATE loadout SET identifier = '{ItemDatabase.UserWeapons[weapId].getPropietary()}', charidentifier = '{charIdentifier}' WHERE id=?",
                        new object[] { weapId });
                p.TriggerEvent("vorpinventory:receiveWeapon", weapId, ItemDatabase.UserWeapons[weapId].getPropietary(),
                    ItemDatabase.UserWeapons[weapId].getName(), ItemDatabase.UserWeapons[weapId].getAllAmmo(), ItemDatabase.UserWeapons[weapId].getAllComponents());
                if (targetIsPlayer && ptarget != null)
                {
                    ptarget.TriggerEvent("vorpCoreClient:subWeapon", weapId);
                }
            }
        }

        private void subWeapon(int player, int weapId)
        {
            Player p = PlayerList[player];

            if (p == null)
            {
                Logger.Error($"subWeapon: Player '{player}' does not exist.");
                return;
            }

            dynamic CoreUser = PluginManager.CORE.getUser(player).getUsedCharacter;
            int charIdentifier = CoreUser.charIdentifier;

            string identifier = "steam:" + p.Identifiers["steam"];
            if (ItemDatabase.UserWeapons.ContainsKey(weapId))
            {
                ItemDatabase.UserWeapons[weapId].setPropietary("");
                Exports["ghmattimysql"]
                    .execute(
                        $"UPDATE loadout SET identifier = '{ItemDatabase.UserWeapons[weapId].getPropietary()}' , charidentifier = '{charIdentifier}' WHERE id=?",
                        new[] { weapId });
            }
            p.TriggerEvent("vorpCoreClient:subWeapon", weapId);
        }


        public static int getUserTotalCount(string identifier)
        {
            int t_count = 0;
            foreach (var item in ItemDatabase.UserInventory[identifier].Values)
            {
                t_count += item.getCount();
            }

            return t_count;
        }

        public static int getUserTotalCountWeapons(string identifier, int charId)
        {
            int t_count = 0;
            foreach (var weapon in ItemDatabase.UserWeapons.Values)
            {
                if (weapon.getPropietary().Contains(identifier) && weapon.getCharId() == charId)
                {
                    t_count += 1;
                }
            }

            return t_count;
        }
    }
}