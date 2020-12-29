﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using MassEffectModManagerCore.modmanager.localizations;
using MassEffectModManagerCore.modmanager.me3tweaks;
using MassEffectModManagerCore.modmanager.objects.nexusfiledb;
using MassEffectModManagerCore.ui;
using ME3ExplorerCore.Helpers;
using Newtonsoft.Json;
using Serilog;

namespace MassEffectModManagerCore.modmanager.usercontrols
{
    /// <summary>
    /// Interaction logic for NexusFileQueryPanel.xaml
    /// </summary>
    public partial class NexusFileQueryPanel : MMBusyPanelBase, INotifyPropertyChanged
    {
        /// <summary>
        /// The API endpoint for searching. Append an encoded filename to search.
        /// </summary>
        public string SearchTerm { get; set; }
        public bool QueryInProgress { get; set; }
        public string StatusText { get; set; }
        public bool SearchME1 { get; set; }
        public bool SearchME2 { get; set; }
        public bool SearchME3 { get; set; }
        public NexusFileQueryPanel()
        {
            LoadCommands();
            InitializeComponent();
        }

        private void LoadCommands()
        {
            SearchCommand = new GenericCommand(PerformSearch, CanSearch);
        }

        public GenericCommand SearchCommand { get; set; }
        public ObservableCollectionExtended<SearchedItemResult> Results { get; } = new ObservableCollectionExtended<SearchedItemResult>();

        public bool HasAPIToken { get; } = APIKeys.HasNexusSearchKey;

        private bool CanSearch() => HasAPIToken && !QueryInProgress && !string.IsNullOrWhiteSpace(SearchTerm) && (SearchME1 || SearchME2 || SearchME3);

        private Dictionary<string, GameDatabase> LoadedDatabases = new Dictionary<string, GameDatabase>();

        private void PerformSearch()
        {
            Results.ClearEx();
            var searchGames = new List<string>();
            if (SearchME1) searchGames.Add(@"masseffect");
            if (SearchME2) searchGames.Add(@"masseffect2");
            if (SearchME3) searchGames.Add(@"masseffect3");
            QueryInProgress = true;
            try
            {
                foreach (var domain in searchGames)
                {
                    if (!LoadedDatabases.TryGetValue(domain, out var db))
                    {
                        db = GameDatabase.LoadDatabase(domain);
                        LoadedDatabases[domain] = db;
                    }

                    // Check if the name exists in filenames. If it doesn't, it will never find it

                    var match = db.NameTable.FirstOrDefault(x =>
                        x.Value.Equals(SearchTerm, StringComparison.InvariantCultureIgnoreCase));

                    if (match.Key != 0)
                    {
                        // Found
                        var instances = db.FileInstances[match.Key];
                        Results.AddRange(instances.Select(x => new SearchedItemResult()
                        {
                            Instance = x,
                            Domain = domain,
                            Filename = db.NameTable[x.FilenameId],
                            AssociatedDB = db
                        }));
                    }
                }

                StatusText = $"{Results.Count} result(s)";
                QueryInProgress = false;
            }
            catch (Exception e)
            {
                Log.Error($@"Could not perform search: {e.Message}");
                QueryInProgress = false;
            }
        }

        public override void HandleKeyPress(object sender, KeyEventArgs e)
        {
        }

        public override void OnPanelVisible()
        {
            //Task.Run(() =>
            //{
            //    try
            //    {
            //        if (APIKeys.HasNexusSearchKey)
            //        {
            //            var latestStatusStr =
            //                OnlineContent.FetchRemoteString($@"{APIEndpoint}status", APIKeys.NexusSearchKey);
            //            var latestStatus = JsonConvert.DeserializeObject<Dictionary<string, double>>(latestStatusStr);
            //            var lastFileIndexing = UnixTimeStampToDateTime(latestStatus[@"last_file_indexing"]);
            //            var lastModIndexing = UnixTimeStampToDateTime(latestStatus[@"last_mod_indexing"]);
            //            StatusText = M3L.GetString(M3L.string_interp_lastIndexing, lastFileIndexing, lastModIndexing);
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        Log.Error($@"Could not get indexing status: {e.Message}");
            //    }
            //});
        }


        private void ClosePanel(object sender, System.Windows.RoutedEventArgs e)
        {
            OnClosing(DataEventArgs.Empty);
        }

        public class APIStatusResult
        {
            public string name { get; set; }
            public double value { get; set; }
        }

        public class SearchedItemResult : INotifyPropertyChanged
        {
            public FileInstance Instance { get; internal set; }
            public string Domain { get; internal set; }
            public string Filename { get; internal set; }
            public GameDatabase AssociatedDB { get; internal set; }

            public string GameIconSource
            {
                get
                {
                    switch (Domain)
                    {
                        case @"masseffect":
                            return @"/images/gameicons/ME1_48.ico";
                        case @"masseffect2":
                            return @"/images/gameicons/ME2_48.ico";
                        case @"masseffect3":
                            return @"/images/gameicons/ME3_48.ico";
                    }

                    return null;
                }
            }

            /// <summary>
            /// The full file path in the archive
            /// </summary>
            public string FullPath
            {
                get
                {
                    if (Instance.ParentPathID == 0) return Filename;
                    return AssociatedDB.Paths[Instance.ParentPathID].GetFullPath(AssociatedDB, Filename);
                }
            }

            /// <summary>
            /// The name of the mod page that has this instance
            /// </summary>
            public string ModName => AssociatedDB.NameTable[Instance.ModNameId];
            /// <summary>
            /// The title of the file on NexusMods that contains this instance within it
            /// </summary>
            public string ModFileTitle => AssociatedDB.NameTable[FileInfo.NameID];

            public NMFileInfo FileInfo => AssociatedDB.ModFileInfos[Instance.FileID];
            
            
#pragma warning disable
            public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink hl && hl.DataContext is SearchedItemResult sir)
            {
                var outboundUrl = $@"https://nexusmods.com/{sir.Domain}/mods/{sir.Instance.ModID}?tab=files";
                Utilities.OpenWebpage(outboundUrl);
            }
        }

        private class SearchTopLevelResult
        {
            public int mod_count { get; set; }
            public string searched_file { get; set; }
            public string file_name { get; set; } // Why?
            public List<string> games { get; set; }
            public List<SearchedItemResult> mod_ids { get; set; }
        }
    }
}
