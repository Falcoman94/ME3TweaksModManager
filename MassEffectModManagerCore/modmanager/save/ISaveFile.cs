﻿using System;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksModManager.modmanager.save.game2.FileFormats;

namespace ME3TweaksModManager.modmanager.save
{
    public interface ISaveFile : IUnrealSerializable
    {
        public MEGame Game { get; }
        public string SaveFilePath { get; set; }
        public DateTime Proxy_TimeStamp { get; }
        public string Proxy_DebugName { get; }
        public IPlayerRecord Proxy_PlayerRecord { get; }
        public string Proxy_BaseLevelName { get; }
        public ESFXSaveGameType SaveGameType { get; set; }
        public uint Version { get; }
        public int SaveNumber { get; set; }

        /// <summary>
        /// If save fully serialized and CRC check passed
        /// </summary>
        bool IsValid { get; set; }
    }
}
