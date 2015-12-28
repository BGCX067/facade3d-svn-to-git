// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.FileSystem;
using System.IO;
using System.Collections;
using GameCommon;
using System.Threading;

namespace GameEntities
{
    /// <summary>
    /// Defines the <see cref="SpeculativeMapChangeRegion"/> entity type.
    /// </summary>
    public class SpeculativeMapChangeRegionType : RegionType
    {
    }

    /// <summary>
    /// Gives an opportunity of moving of the player between maps.
    /// When the player gets in the center of this region the game loads a new map.
    /// </summary>
    public class SpeculativeMapChangeRegion : Region
    {
        [FieldSerialize]
        string mapName;
        [FieldSerialize]
        string spawnPointName;
        [FieldSerialize]
        bool enabled = true;

        Region region1;
        Region region2;
        Region region3;
        Region region4;
        Region region5;
        Region region6;
        Region region7;
        Region region8;
        List<EntityType> entitiesToPreload;
        static List<EntityType> Region8List;
        static List<EntityType> Region7List;
        static List<EntityType> Region6List;
        static List<EntityType> Region5List;
        static List<EntityType> Region4List;
        static List<EntityType> Region3List;
        static List<EntityType> Region2List;
        static List<EntityType> Region1List;
        static bool region1Preloaded = false;
        static bool region2Preloaded = false;
        static bool region3Preloaded = false;
        static bool region4Preloaded = false;
        static bool region5Preloaded = false;
        static bool region6Preloaded = false;
        static bool region7Preloaded = false;
        static bool region8Preloaded = false;
        static bool region1Unloading = false;
        static bool region2Unloading = false;
        static bool region3Unloading = false;
        static bool region4Unloading = false;
        static bool region5Unloading = false;
        static bool region6Unloading = false;
        static bool region7Unloading = false;
        static bool region8Unloading = false;
        static Thread BL;
        static Thread R1L;
        static Thread R2L;
        static Thread R3L;
        static Thread R4L;
        static Thread R5L;
        static Thread R6L;
        static Thread R7L;
        static Thread R8L;
        static Thread R1U;
        static Thread R2U;
        static Thread R3U;
        static Thread R4U;
        static Thread R5U;
        static Thread R6U;
        static Thread R7U;
        static Thread R8U;
        static bool RegionsFilled = false;
        static bool started = false;

        //

        SpeculativeMapChangeRegionType _type = null; public new SpeculativeMapChangeRegionType Type { get { return _type; } }

        /// <summary>
        /// Gets or sets the name of a map for loading.
        /// </summary>
        [Description("Name of a map for loading.")]
        public string MapName
        {
            get { return mapName; }
            set { mapName = value; }
        }

        /// <summary>
        /// Gets or set the name of a spawn point in the destination map.
        /// </summary>
        [Description("The name of a spawn point in the destination map.")]
        public string SpawnPointName
        {
            get { return spawnPointName; }
            set { spawnPointName = value; }
        }

        /// <summary>
        /// Decides whether or not the player can activate this region.
        /// </summary>
        [DefaultValue(true)]
        [Description("Can the player activate this region?")]
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);
            region1 = (Region)Entities.Instance.Create("Region", Map.Instance);
            region1.Position = this.Position;
            region1.Scale = new Vec3(this.Scale.X + 4, this.Scale.Y + 4, this.Scale.Z + 4);
            region1.AllowSave = false;
            region1.EditorSelectable = false;
            region1.PostCreate();

            region2 = (Region)Entities.Instance.Create("Region", Map.Instance);
            region2.Position = this.Position;
            region2.Scale = new Vec3(this.Scale.X + 8, this.Scale.Y + 8, this.Scale.Z + 8);
            region2.AllowSave = false;
            region2.EditorSelectable = false;
            region2.PostCreate();

            region3 = (Region)Entities.Instance.Create("Region", Map.Instance);
            region3.Position = this.Position;
            region3.Scale = new Vec3(this.Scale.X + 12, this.Scale.Y + 12, this.Scale.Z + 12);
            region3.AllowSave = false;
            region3.EditorSelectable = false;
            region3.PostCreate();

            region4 = (Region)Entities.Instance.Create("Region", Map.Instance);
            region4.Position = this.Position;
            region4.Scale = new Vec3(this.Scale.X + 16, this.Scale.Y + 16, this.Scale.Z + 16);
            region4.AllowSave = false;
            region4.EditorSelectable = false;
            region4.PostCreate();

            region5 = (Region)Entities.Instance.Create("Region", Map.Instance);
            region5.Position = this.Position;
            region5.Scale = new Vec3(this.Scale.X + 20, this.Scale.Y + 20, this.Scale.Z + 20);
            region5.AllowSave = false;
            region5.EditorSelectable = false;
            region5.PostCreate();

            region6 = (Region)Entities.Instance.Create("Region", Map.Instance);
            region6.Position = this.Position;
            region6.Scale = new Vec3(this.Scale.X + 24, this.Scale.Y + 24, this.Scale.Z + 24);
            region6.AllowSave = false;
            region6.EditorSelectable = false;
            region6.PostCreate();

            region7 = (Region)Entities.Instance.Create("Region", Map.Instance);
            region7.Position = this.Position;
            region7.Scale = new Vec3(this.Scale.X + 28, this.Scale.Y + 28, this.Scale.Z + 28);
            region7.AllowSave = false;
            region7.EditorSelectable = false;
            region7.PostCreate();

            region8 = (Region)Entities.Instance.Create("Region", Map.Instance);
            region8.Position = this.Position;
            region8.Scale = new Vec3(this.Scale.X + 32, this.Scale.Y + 32, this.Scale.Z + 32);
            region8.AllowSave = false;
            region8.EditorSelectable = false;
            region8.PostCreate();

            region1.ObjectIn += new ObjectInOutDelegate(region1_ObjectIn);
            region2.ObjectIn += new ObjectInOutDelegate(region2_ObjectIn);
            region3.ObjectIn += new ObjectInOutDelegate(region3_ObjectIn);
            region4.ObjectIn += new ObjectInOutDelegate(region4_ObjectIn);
            region5.ObjectIn += new ObjectInOutDelegate(region5_ObjectIn);
            region6.ObjectIn += new ObjectInOutDelegate(region6_ObjectIn);
            region7.ObjectIn += new ObjectInOutDelegate(region7_ObjectIn);
            region8.ObjectIn += new ObjectInOutDelegate(region8_ObjectIn);
            region1.ObjectOut += new ObjectInOutDelegate(region1_ObjectOut);
            region2.ObjectOut += new ObjectInOutDelegate(region2_ObjectOut);
            region3.ObjectOut += new ObjectInOutDelegate(region3_ObjectOut);
            region4.ObjectOut += new ObjectInOutDelegate(region4_ObjectOut);
            region5.ObjectOut += new ObjectInOutDelegate(region5_ObjectOut);
            region6.ObjectOut += new ObjectInOutDelegate(region6_ObjectOut);
            region7.ObjectOut += new ObjectInOutDelegate(region7_ObjectOut);
            region8.ObjectOut += new ObjectInOutDelegate(region8_ObjectOut);
            ObjectIn += new ObjectInOutDelegate(MapChangeRegion_ObjectIn);
        }

        protected override void OnSetTransform(ref Vec3 pos, ref Quat rot, ref Vec3 scl)
        {
            base.OnSetTransform(ref pos, ref rot, ref scl);
            if (this.IsPostCreated)
            {
                region1.Position = this.Position;
                region1.Scale = new Vec3(this.Scale.X + 4, this.Scale.Y + 4, this.Scale.Z + 4);
                region2.Position = this.Position;
                region2.Scale = new Vec3(this.Scale.X + 8, this.Scale.Y + 8, this.Scale.Z + 8);
                region3.Position = this.Position;
                region3.Scale = new Vec3(this.Scale.X + 12, this.Scale.Y + 12, this.Scale.Z + 12);
                region4.Position = this.Position;
                region4.Scale = new Vec3(this.Scale.X + 16, this.Scale.Y + 16, this.Scale.Z + 16);
                region5.Position = this.Position;
                region5.Scale = new Vec3(this.Scale.X + 20, this.Scale.Y + 20, this.Scale.Z + 20);
                region6.Position = this.Position;
                region6.Scale = new Vec3(this.Scale.X + 24, this.Scale.Y + 24, this.Scale.Z + 24);
                region7.Position = this.Position;
                region7.Scale = new Vec3(this.Scale.X + 28, this.Scale.Y + 28, this.Scale.Z + 28);
                region8.Position = this.Position;
                region8.Scale = new Vec3(this.Scale.X + 32, this.Scale.Y + 32, this.Scale.Z + 32);
            }
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
        protected override void OnDestroy()
        {
            region1.ObjectIn -= new ObjectInOutDelegate(region1_ObjectIn);
            region2.ObjectIn -= new ObjectInOutDelegate(region2_ObjectIn);
            region3.ObjectIn -= new ObjectInOutDelegate(region3_ObjectIn);
            region4.ObjectIn -= new ObjectInOutDelegate(region4_ObjectIn);
            region5.ObjectIn -= new ObjectInOutDelegate(region5_ObjectIn);
            region6.ObjectIn -= new ObjectInOutDelegate(region6_ObjectIn);
            region7.ObjectIn -= new ObjectInOutDelegate(region7_ObjectIn);
            region8.ObjectIn -= new ObjectInOutDelegate(region8_ObjectIn);
            region1.ObjectOut -= new ObjectInOutDelegate(region1_ObjectOut);
            region2.ObjectOut -= new ObjectInOutDelegate(region2_ObjectOut);
            region3.ObjectOut -= new ObjectInOutDelegate(region3_ObjectOut);
            region4.ObjectOut -= new ObjectInOutDelegate(region4_ObjectOut);
            region5.ObjectOut -= new ObjectInOutDelegate(region5_ObjectOut);
            region6.ObjectOut -= new ObjectInOutDelegate(region6_ObjectOut);
            region7.ObjectOut -= new ObjectInOutDelegate(region7_ObjectOut);
            region8.ObjectOut -= new ObjectInOutDelegate(region8_ObjectOut);
            region1.SetShouldDelete();
            region2.SetShouldDelete();
            region3.SetShouldDelete();
            region4.SetShouldDelete();
            region5.SetShouldDelete();
            region6.SetShouldDelete();
            region7.SetShouldDelete();
            region8.SetShouldDelete();
            ObjectIn -= new ObjectInOutDelegate(MapChangeRegion_ObjectIn);
            base.OnDestroy();
        }

        void region1_ObjectIn(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (!region1Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region1Preloaded = true;
                        R1L = new Thread(() => PreloadRegion1());
                        R1L.IsBackground = true;
                        R1L.Start();
                    }
                }
            }
        }

        void region2_ObjectIn(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (!region2Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region2Preloaded = true;
                        R2L = new Thread(() => PreloadRegion2());
                        R2L.IsBackground = true;
                        R2L.Start();
                    }
                }
            }
        }

        void region3_ObjectIn(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (!region3Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region3Preloaded = true;
                        R3L = new Thread(() => PreloadRegion3());
                        R3L.IsBackground = true;
                        R3L.Start();
                    }
                }
            }
        }

        void region4_ObjectIn(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (!region4Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region4Preloaded = true;
                        R4L = new Thread(() => PreloadRegion4());
                        R4L.IsBackground = true;
                        R4L.Start();
                    }
                }
            }
        }

        void region5_ObjectIn(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (!region5Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region5Preloaded = true;
                        R5L = new Thread(() => PreloadRegion5());
                        R5L.IsBackground = true;
                        R5L.Start();
                    }
                }
            }
        }

        void region6_ObjectIn(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (!region6Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region6Preloaded = true;
                        R6L = new Thread(() => PreloadRegion6());
                        R6L.IsBackground = true;
                        R6L.Start();
                    }
                }
            }
        }

        void region7_ObjectIn(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (!region7Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region7Preloaded = true;
                        R7L = new Thread(() => PreloadRegion7());
                        R7L.IsBackground = true;
                        R7L.Start();
                    }
                }
            }
        }

        void region8_ObjectIn(Entity entity, MapObject obj)
        {
            if (enabled)
            {
                if (RegionsFilled)
                {
                    if (!region8Preloaded)
                    {
                        if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                        {
                            region8Preloaded = true;
                            R8L = new Thread(() => PreloadRegion8());
                            R8L.IsBackground = true;
                            R8L.Start();
                        }
                    }
                }
                else if (!started)
                {
                    started = true;
                    BL = new Thread(() => FillObjectList(MapName));
                    BL.IsBackground = true;
                    BL.Start();
                }
            }
        }

        void region1_ObjectOut(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (region1Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region1Preloaded = false;
                        region1Unloading = true;
                        R1U = new Thread(() => UnPreloadRegion1());
                        R1U.IsBackground = true;
                        R1U.Start();
                    }
                }
            }
        }

        void region2_ObjectOut(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (region2Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region2Preloaded = false;
                        region2Unloading = true;
                        R2U = new Thread(() => UnPreloadRegion2());
                        R2U.IsBackground = true;
                        R2U.Start();
                    }
                }
            }
        }

        void region3_ObjectOut(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (region3Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region3Preloaded = false;
                        region3Unloading = true;
                        R3U = new Thread(() => UnPreloadRegion3());
                        R3U.IsBackground = true;
                        R3U.Start();
                    }
                }
            }
        }

        void region4_ObjectOut(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (region4Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region4Preloaded = false;
                        region4Unloading = true;
                        R4U = new Thread(() => UnPreloadRegion4());
                        R4U.IsBackground = true;
                        R4U.Start();
                    }
                }
            }
        }

        void region5_ObjectOut(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (region5Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region5Preloaded = false;
                        region5Unloading = true;
                        R5U = new Thread(() => UnPreloadRegion5());
                        R5U.IsBackground = true;
                        R5U.Start();
                    }
                }
            }
        }

        void region6_ObjectOut(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (region6Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region6Preloaded = false;
                        region6Unloading = true;
                        R6U = new Thread(() => UnPreloadRegion6());
                        R6U.IsBackground = true;
                        R6U.Start();
                    }
                }
            }
        }

        void region7_ObjectOut(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (region7Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region7Preloaded = false;
                        region7Unloading = true;
                        R7U = new Thread(() => UnPreloadRegion7());
                        R7U.IsBackground = true;
                        R7U.Start();
                    }
                }
            }
        }

        void region8_ObjectOut(Entity entity, MapObject obj)
        {
            if (RegionsFilled)
            {
                if (region8Preloaded)
                {
                    if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                    {
                        region8Preloaded = false;
                        region8Unloading = true;
                        R8U = new Thread(() => UnPreloadRegion8());
                        R8U.IsBackground = true;
                        R8U.Start();
                    }
                }
            }
        }

        private void PreloadRegion1()
        {
            foreach (EntityType item in Region1List)
            {
                if (!region1Unloading)
                {
                    item.PreloadResources();
                }
                else
                {
                    region1Preloaded = false;
                    break;
                }
            }
            EngineConsole.Instance.Print("Region 1 Preloaded");
        }

        private void PreloadRegion2()
        {
            foreach (EntityType item in Region2List)
            {
                item.PreloadResources();
                if (!region2Unloading)
                {
                    item.PreloadResources();
                }
                else
                {
                    region2Preloaded = false;
                    break;
                }
            }
            EngineConsole.Instance.Print("Region 2 Preloaded");
        }

        private void PreloadRegion3()
        {
            foreach (EntityType item in Region3List)
            {
                if (!region3Unloading)
                {
                    item.PreloadResources();
                }
                else
                {
                    region3Preloaded = false;
                    break;
                }
            }
            EngineConsole.Instance.Print("Region 3 Preloaded");
        }

        private void PreloadRegion4()
        {
            foreach (EntityType item in Region4List)
            {
                if (!region4Unloading)
                {
                    item.PreloadResources();
                }
                else
                {
                    region4Preloaded = false;
                    break;
                }
            }
            EngineConsole.Instance.Print("Region 4 Preloaded");
        }

        private void PreloadRegion5()
        {
            foreach (EntityType item in Region5List)
            {
                if (!region5Unloading)
                {
                    item.PreloadResources();
                }
                else
                {
                    region5Preloaded = false;
                    break;
                }
            }
            EngineConsole.Instance.Print("Region 5 Preloaded");
        }

        private void PreloadRegion6()
        {
            foreach (EntityType item in Region6List)
            {
                if (!region6Unloading)
                {
                    item.PreloadResources();
                }
                else
                {
                    region6Preloaded = false;
                    break;
                }
            }
            EngineConsole.Instance.Print("Region 6 Preloaded");
        }

        private void PreloadRegion7()
        {
            foreach (EntityType item in Region7List)
            {
                if (!region7Unloading)
                {
                    item.PreloadResources();
                }
                else
                {
                    region7Preloaded = false;
                    break;
                }
            }
            EngineConsole.Instance.Print("Region 7 Preloaded");
        }

        private void PreloadRegion8()
        {
            foreach (EntityType item in Region8List)
            {
                if (!region8Unloading)
                {
                    item.PreloadResources();
                }
                else
                {
                    region8Preloaded = false;
                    break;
                }
            }
            EngineConsole.Instance.Print("Region 8 Preloaded");
        }

        private void UnPreloadRegion1()
        {
            foreach (EntityType item in Region1List)
            {
                item.Dispose();
            }
            region1Unloading = false;
            EngineConsole.Instance.Print("Region 1 UnPreloaded");
        }

        private void UnPreloadRegion2()
        {
            foreach (EntityType item in Region2List)
            {
                item.Dispose();
            }
            region2Unloading = false;
            EngineConsole.Instance.Print("Region 2 UnPreloaded");
        }

        private void UnPreloadRegion3()
        {
            foreach (EntityType item in Region3List)
            {
                item.Dispose();
            }
            region3Unloading = false;
            EngineConsole.Instance.Print("Region 3 UnPreloaded");
        }

        private void UnPreloadRegion4()
        {
            foreach (EntityType item in Region4List)
            {
                item.Dispose();
            }
            region4Unloading = false;
            EngineConsole.Instance.Print("Region 4 UnPreloaded");
        }

        private void UnPreloadRegion5()
        {
            foreach (EntityType item in Region5List)
            {
                item.Dispose();
            }
            region5Unloading = false;
            EngineConsole.Instance.Print("Region 5 UnPreloaded");
        }

        private void UnPreloadRegion6()
        {
            foreach (EntityType item in Region6List)
            {
                item.Dispose();
            }
            region6Unloading = false;
            EngineConsole.Instance.Print("Region 6 UnPreloaded");
        }

        private void UnPreloadRegion7()
        {
            foreach (EntityType item in Region7List)
            {
                item.Dispose();
            }
            region7Unloading = false;
            EngineConsole.Instance.Print("Region 7 UnPreloaded");
        }

        private void UnPreloadRegion8()
        {
            foreach (EntityType item in Region8List)
            {
                item.Dispose();
            }
            region8Unloading = false;
        }

        private void FillObjectList(string MapName)
        {
            string realPath = VirtualFileSystem.ResourceDirectoryPath + "\\" + MapName;
            StreamReader rdr = new StreamReader(realPath);
            string mapFileData = rdr.ReadToEnd();
            ArrayList typeListLocations = Searching.KnuthMorisPlat.GetAllOccurences("   type = ", mapFileData);
            mapFileData = "";
            ArrayList typeList = new ArrayList();
            foreach (Int32 loc in typeListLocations)
            {
                rdr.DiscardBufferedData();
                rdr.BaseStream.Seek(loc, SeekOrigin.Begin);
                string ln = rdr.ReadLine();
                string type = ln.Substring(8);
                typeList.Add(type);
            }
            typeListLocations.Clear();
            rdr.Close();
            rdr.Dispose();
            ArrayList cleanList = RemoveDups(typeList);
            typeList.Clear();
            String[] tmp = (String[])cleanList.ToArray(typeof(String));
            List<string> CleanEntityList = new List<string>(tmp);
            cleanList.Clear();
            entitiesToPreload = new List<EntityType>();
            foreach (string entityType in CleanEntityList)
            {
                entitiesToPreload.Add(EntityTypes.Instance.GetByName(entityType));
            }
            CleanEntityList.Clear();
            int NumberOfTypesToPreload = entitiesToPreload.Count;
            int currentTypeNumber = 0;
            //We have to use Math.Floor here so that we know that we will always
            //be under the total number of object we need to load, otherwise we
            //would be trying to preload types that don't exist.
            Region1List = new List<EntityType>();
            Region2List = new List<EntityType>();
            Region3List = new List<EntityType>();
            Region4List = new List<EntityType>();
            Region5List = new List<EntityType>();
            Region6List = new List<EntityType>();
            Region7List = new List<EntityType>();
            Region8List = new List<EntityType>();
            while (currentTypeNumber < Math.Floor((decimal)(NumberOfTypesToPreload / 8)))
            {
                Region1List.Add(entitiesToPreload[currentTypeNumber]);
                currentTypeNumber = currentTypeNumber + 1;
            }
            while (currentTypeNumber < 2 * (Math.Floor((decimal)(NumberOfTypesToPreload / 8))))
            {
                Region2List.Add(entitiesToPreload[currentTypeNumber]);
                currentTypeNumber = currentTypeNumber + 1;
            }
            while (currentTypeNumber < 3 * (Math.Floor((decimal)(NumberOfTypesToPreload / 8))))
            {
                Region3List.Add(entitiesToPreload[currentTypeNumber]);
                currentTypeNumber = currentTypeNumber + 1;
            }
            while (currentTypeNumber < 4 * (Math.Floor((decimal)(NumberOfTypesToPreload / 8))))
            {
                Region4List.Add(entitiesToPreload[currentTypeNumber]);
                currentTypeNumber = currentTypeNumber + 1;
            }
            while (currentTypeNumber < 5 * (Math.Floor((decimal)(NumberOfTypesToPreload / 8))))
            {
                Region5List.Add(entitiesToPreload[currentTypeNumber]);
                currentTypeNumber = currentTypeNumber + 1;
            }
            while (currentTypeNumber < 6 * (Math.Floor((decimal)(NumberOfTypesToPreload / 8))))
            {
                Region6List.Add(entitiesToPreload[currentTypeNumber]);
                currentTypeNumber = currentTypeNumber + 1;
            }
            while (currentTypeNumber < 7 * (Math.Floor((decimal)(NumberOfTypesToPreload / 8))))
            {
                Region7List.Add(entitiesToPreload[currentTypeNumber]);
                currentTypeNumber = currentTypeNumber + 1;
            }
            while (currentTypeNumber < NumberOfTypesToPreload)
            {
                Region8List.Add(entitiesToPreload[currentTypeNumber]);
                currentTypeNumber = currentTypeNumber + 1;
            }
            entitiesToPreload.Clear();
            RegionsFilled = true;
        }

        void MapChangeRegion_ObjectIn(Entity entity, MapObject obj)
        {
            if (enabled)
            {
                if (PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj)
                {
                    if (EntitySystemWorld.Instance.IsServer())
                    {
                        Log.Warning("MapChangeRegion: Networking mode is not supported.");
                        return;
                    }

                    PlayerCharacter character = (PlayerCharacter)PlayerIntellect.Instance.ControlledObject;
                    MapChangeRegion changeRegion = new MapChangeRegion();
                    changeRegion.MapName = this.mapName;
                    changeRegion.SpawnPointName = this.spawnPointName;
                    PlayerCharacter.ChangeMapInformation playerCharacterInformation = character.GetChangeMapInformation(changeRegion);
                    GameWorld.Instance.SetShouldChangeMap(mapName, spawnPointName, playerCharacterInformation);
                }
            }
        }

        public ArrayList RemoveDups(ArrayList items)
        {
            ArrayList noDups = new ArrayList();
            foreach (string strItem in items)
            {
                if (!noDups.Contains(strItem.Trim()))
                {
                    noDups.Add(strItem.Trim());
                }
            }
            noDups.Sort();
            return noDups;
        }
    }
}