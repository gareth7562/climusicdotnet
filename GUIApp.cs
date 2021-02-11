using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal.Gui;
using LibVLCSharp.Shared;


namespace CLIMusicDotNet
{

    class GUIApp
    {

        Label selectedTrackTitle = new Label("Choose a track...") { Width = Dim.Fill() - 1, Y = 2 };
        Label selectedTrackArtist = new Label() { Width = Dim.Fill() - 1, Y = 1};

        int currentTrack = 0;
        List<String> musicFiles;
        FrameView trackPlaying;
        Label timerText = new Label("Current Time") { Width = Dim.Fill(), Y = 4 };
        string[] modes = { "Continuous", "Shuffle" };
        Label modeText;
        bool shuffle;
        List<string> playList;
        string musicDir = @".";
        TextField entry;
        ListView MusicView;
        ListView PlayListView;
        Toplevel top;
        Terminal.Gui.Dialog dialog, url_dialog;
        Random r = new Random();
        bool dirSet = false;
        object token;
        bool token_created = false;
        MediaPlayer mp;
        LibVLCSharp.Shared.LibVLC _libVLC;
        ProgressBar progress;
        Button pauseButton = new Button(10, 8, "Pause");
        LastPlayedTrack lastPlayedTrack = new LastPlayedTrack();
        Label dirText;
        List<String> dirnames = new List<string>();
        List<String> filelist = new List<string>();

        List<Track> playListTable = new List<Track>();

        public void Init()
        {
            Application.Init();

            Application.UseSystemConsole = true; 
            top = Application.Top;
            modeText = new Label(modes[0].ToString());
            shuffle = false;
            LibVLCSharp.Shared.Core.Initialize();
            
            _libVLC = new LibVLCSharp.Shared.LibVLC("-I", "null");
            mp = new MediaPlayer(_libVLC); 
            mp.TimeChanged += mp_TimeChanged;

            playList = new List<string>();
        }

        private void mp_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {

		    var current_progress = (float)((double)e.Time / (double)mp.Length); 


	        timerText.Text = TimeSpan.FromMilliseconds(e.Time).ToString().Substring(0, 8) + "/" + TimeSpan.FromMilliseconds(mp.Length).ToString().Substring(0, 8) + " (" + e.Time + "/" + mp.Length + ")" + " " + Convert.ToInt32(current_progress * 100) + "%";
		    progress.Fraction = current_progress;


        }


        public void AddControls()
        {

            MusicView = new ListView(musicFiles);
            PlayListView = new ListView(playList);

            MusicView.Width = Dim.Fill() - 1;
            MusicView.Height = Dim.Fill();

            PlayListView.Width = Dim.Fill() - 1;
            PlayListView.Height = Dim.Fill();

            Button playButton = new Button(1, 8, "Play");

            Button set_button = new Button(5, 5, "Ok");
            Button set_url_button = new Button(5, 5, "Ok");


            Button bookMarkButton = new Button(30, 8, "Bookmark");
            Button skipButton = new Button(20, 8, "Skip");

            skipButton.Clicked += () => {

                if(playList.Count > 0)
                nextTrack();
            
            };
            dirText = new Label(new Rect(1, 1, 256, 20), "Current Directory");
            var sItemPause = new StatusItem(Key.Space, "SPACE - Pause/Resume", ()=> {
                
                if(mp.IsPlaying)
                {
                    mp.SetPause(true);
                    pauseButton.Text = "Resume";
                }
                else
                {
                    mp.SetPause(false);
                    pauseButton.Text = "Pause";
                }

            });

            var sItemFastForward = new StatusItem(Key.ControlF, "Ctrl + F - Fast Forward", () => {
                mp.Position += 0.01F;
            });

            var sItemSkipTrack = new StatusItem(Key.ControlS, "Ctrl + S - Skip Track", () => {
                nextTrack();

            });

            StatusItem[] statusItems = {sItemPause, sItemFastForward, sItemSkipTrack};
            var statusBar = new StatusBar(statusItems);


            playButton.Clicked += () => {
                if(playList.Count > 0)
                playTrack();
                else
                    MessageBox.ErrorQuery("Playlist Empty", "Press Escape to continue");
            };

            bookMarkButton.Clicked += () => {
                mp.SetPause(true);
                lastPlayedTrack.write(playListTable, mp.Time, currentTrack, musicDir);
            };

            pauseButton.Clicked += () => {

                if(pauseButton.Text == "Resume")
                {
                    mp.SetPause(false);
                    pauseButton.Text = "Pause";
                }
                else
                {
                    mp.SetPause(true);
                    pauseButton.Text = "Resume";
                }
            };

            MusicView.OpenSelectedItem += openDialogClicked;
            MusicView.SelectedItemChanged += musicViewSelect;
            PlayListView.OpenSelectedItem += playListClicked;

            dialog = new Terminal.Gui.Dialog("Enter Music Directory", 50, 10, set_button);
            url_dialog = new Terminal.Gui.Dialog("Enter URL", 50, 10, set_url_button);
            url_dialog.Visible = false;

            entry = new TextField()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(),
                Height = 1
            };

            var url_entry = new TextField()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(),
                Height = 1
            };

            url_dialog.Add(url_entry);
            dialog.Add(entry);

            var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem ("_Open Music DIR", "", () => {
                    dialog.Visible = true;
                    entry.SetFocus();
                }),

                  new MenuItem ("_Add All", "", () => {
                       
                    foreach(string filename in filelist)
                    {
                        playList.Add(Path.GetFileName(filename));
                        var track = new Track();
                        track.title = Path.GetFileName(filename);
                        track.directory = filename;
                        playListTable.Add(track);
                    }
                    PlayListView.SetSource(playList);
                    
                }),

                   new MenuItem ("_Open URL", "", () => {
                    url_dialog.Visible = true;
                    url_entry.SetFocus();

                }),

                  new MenuItem ("_Exit","", () => {
                      mp.SetPause(true);
                      lastPlayedTrack.write(playListTable, mp.Time, currentTrack, musicDir);
                      top.Running = false;

                      cleanUp();

                      
                      
                })
            }),

            new MenuBarItem ("_Playlist", new MenuItem [] {
                new MenuItem ("_Play","", () => {
                    if(playList.Count >= 1 && PlayListView.SelectedItem > 0)
                    {
                        currentTrack = PlayListView.SelectedItem;
                    }
                    else
                    {
                        currentTrack = 0;
                    }
                    if(playList.Count != 0)
                    playTrack();

                }),
                new MenuItem("_Clear","", () => {
                    mp.Stop();
                    playList.Clear();
                    playListTable.Clear();
                    PlayListView.SetSource(playList);
                    currentTrack = 0;


                }),

            }),

            new MenuBarItem ("_Mode", new MenuItem [] {
                new MenuItem ("_Shuffle","", () => {
                    shuffle = true;
                    modeText.Text = modes[1].ToString();
                    nextTrack();

                }),
                new MenuItem("_Continuous","", () =>
                {
                    shuffle = false;
                    modeText.Text = modes[0].ToString();
                }),

            }),
        });

            top.Add(menu);


            set_button.Clicked += buttonClicked;
            set_url_button.Clicked += () => {

                
            if(url_entry.Text.Length <= 0)
            {

                url_dialog.Visible = false;
                MessageBox.ErrorQuery("Invalid URL", "Press Escape to continue");
                PlayListView.SetFocus();
                return;
            }
            playList.Add(url_entry.Text.ToString());
            var track = new Track();
            track.title = url_entry.Text.ToString();
            track.directory = url_entry.Text.ToString();

            playListTable.Add(track);
            url_dialog.Visible = false;
            };
            
            if(!dirSet)
            {
                dialog.Visible = false;
                listDirContents();
            }


            trackPlaying = new FrameView("Current Track")
            {
                X = 0,
                Y = 1,

                Height = Dim.Percent(40, true),
                Width = Dim.Percent(48, true)

            };

           
                      
            var PlayListWindow = new FrameView("PlayList")
            {
                X = 0,
                Y = Pos.Bottom(trackPlaying),
                Height = Dim.Percent(80, true),
                Width = Dim.Percent(48, true)
            };

          
            var progressFrame = new FrameView()
            {
                X = 0,
                Y = 5,
                Width = Dim.Fill(),
                Height = 3,
                CanFocus = false
            };




            progress = new ProgressBar() {
                X = 0,
                Y = 0,
                Width = Dim.Fill() + 1,
                Height = 1,
            };
	        progress.Fraction = 0F;
            PlayListWindow.Add(PlayListView);


            progressFrame.Add(progress);
            trackPlaying.Add(
                selectedTrackTitle,
                selectedTrackArtist,
                timerText,
                modeText
                );
            var win = new FrameView("Music Files")
            {
                X = Pos.Right(trackPlaying),
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Percent(88, true)

            };
            win.Add(   
                      MusicView

                        );

            var currentDirWindow = new FrameView("Currently Browsing")
            {
                X = 0,
                Y = Pos.Bottom(win),
                Width = Dim.Percent(100, true),
                Height = Dim.Percent(90, true)
                
            };
            entry.SetFocus();

            top.Add(trackPlaying);
            top.Add(win);
            top.Add(PlayListWindow, dialog, statusBar, url_dialog);
            trackPlaying.Add(bookMarkButton, progressFrame, playButton, pauseButton, skipButton);
            timerText.Text = "";

            currentDirWindow.Add(dirText);
            top.Add(currentDirWindow);

            top.Width = Dim.Percent(100, true);
            top.Height = Dim.Percent(100, true);
            lastPlayedTrack.load();
            if(lastPlayedTrack.playlist_exists)
            {
                playList.Clear();

                playListTable = lastPlayedTrack.getPlayListFiles();
                foreach(Track t in playListTable) {
                    playList.Add(t.title);
                }
                PlayListView.SetSource(playList);

                if(playList.Count > 0)
                {
                    currentTrack = lastPlayedTrack.getPlaylistPosition();
                    playTrack();
                    
                    mp.Time = lastPlayedTrack.getTrackTime();
                    
                }
                if(lastPlayedTrack.file_exists)
                musicDir = lastPlayedTrack.getLastDirectory();
                listDirContents();

            }

            Application.Resized += windowResized;

            Application.Run();

        }

        void windowResized(Application.ResizedEventArgs args)
        {
            top.Height = Dim.Fill();
            top.Width = Dim.Fill();
            
        }
        
        private void listDirContents()
        {

            var denied_string = "Access Denied";
            var dir_invalid_string = "Invalid Directory";
            var key_hint_string = "Press Escape to Continue.";


            string[] dirs = {};
            try {
                dirs = Directory.GetDirectories(musicDir);
            }
            catch (UnauthorizedAccessException)
            {
                dialog.Visible = false;
                MessageBox.ErrorQuery(denied_string, key_hint_string);

                return;
            }
            catch(DirectoryNotFoundException)
            {
                dialog.Visible = false;
                MessageBox.ErrorQuery(dir_invalid_string, key_hint_string);
                return;
            }

            string[] fileTypes = {".mp3", ".flac", ".m4a", ".ogg", ".wav", ".aac", ".opus"};

            musicFiles = Directory.GetFiles(musicDir, "*.*").ToList().Where(f => fileTypes.Contains(new FileInfo(f).Extension.ToLower())).ToList();
            
            filelist.Clear();

            var filenames = new List<String>();
            dirnames = new List<String>();
            foreach(string file in musicFiles) {
                filenames.Add(Path.GetFileName(file));
                filelist.Add(file);
            }
            dirnames.Insert(0, "...");
            filenames.Insert(0, "...");

            foreach (var dir in dirs)
            {

                var dirName = Path.GetFileName(dir) + "/";
                dirnames.Insert(1, dir);
                filenames.Insert(1, dirName);

            }


            dirText.Text = new DirectoryInfo(musicDir).Name + " on " + Path.GetPathRoot(musicDir);

            MusicView.SetSource(filenames);
            MusicView.SetFocus();

        }
        private void buttonClicked()
        {
            musicDir = @entry.Text.ToString();
            if(musicDir.Length == 0)
            {
                MessageBox.ErrorQuery("Invalid Directory", "Press Escape to Continue");
                return;
            }
            listDirContents();
            dialog.Visible = false;
            MusicView.SetFocus();
    

        }

        private void musicViewSelect(ListViewItemEventArgs obj)
        {
            if(obj.Value.ToString() == "...")
            {
                dirText.Text = "Go up a directory";
                return;
            }
            dirText.Text = new DirectoryInfo(obj.Value.ToString()).Name + " on " + Path.GetPathRoot(musicDir);
        }
        private void openDialogClicked(ListViewItemEventArgs obj)
        {

                if(obj.Value.ToString() != "...")
		        {
                    if(dirnames.Count > 0 && obj.Item <= dirnames.Count - 1)
                    {
                            FileAttributes attr = File.GetAttributes(dirnames[obj.Item]);
                        if((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                                musicDir = dirnames[obj.Item];
                                listDirContents();        
                                return;
                        }
                    }
		        }

                if(obj.Value.ToString() != "...")
                {



                    dirText.Text = musicDir.ToString();
                    var filename = Path.GetFileName(filelist[obj.Item - dirnames.Count]);
                    playList.Add(filename);
                    var track = new Track();
                    track.title = filename;
                    track.directory = filelist[obj.Item - dirnames.Count];
                    playListTable.Add(track);
                    PlayListView.SetSource(playList);
                }
                else
                {
                    
                    if(Directory.GetDirectoryRoot(musicDir) != musicDir)
                    {
                        var temp = Directory.GetParent(musicDir).ToString();
                        musicDir = temp;
                        listDirContents();
                    }
                }

        }

        private void nextTrack()
        {


            if(shuffle)
            {
                var next_track = r.Next(playList.Count - 1);
                currentTrack = next_track;
            }
            else
            {

                 if(currentTrack < playList.Count - 1)
                 {
                    currentTrack += 1;

                 }
                else
                {
                    currentTrack = 0;
                }
            }
            playTrack();


        }

        private void playListClicked(ListViewItemEventArgs obj)
        {

            int selected_Item = obj.Item;
            if(selected_Item.ToString().Length == 0)
                return;

            selectedTrackTitle.Text = Path.GetFileName(playList[selected_Item].ToString());
            currentTrack = selected_Item;
            playTrack();
        }

        private async void playTrack()
        {

            selectedTrackTitle.Text = Path.GetFileName(playList[currentTrack]);
            progress.Fraction = 0F; 
            
            Media media1;

            if(playList[currentTrack].StartsWith("https://"))
            {
                media1 = new Media(_libVLC, playListTable[currentTrack].title, FromType.FromLocation);
                await media1.Parse(MediaParseOptions.ParseNetwork);
                mp.Play(media1);
            }
            else
            {
                media1 = new Media(_libVLC, playListTable[currentTrack].directory, FromType.FromPath);
                mp.Play(media1);
                mp.Position = 0;
                await media1.Parse(MediaParseOptions.ParseLocal);
                var artist = media1.Meta(MetadataType.Artist);
                var title = media1.Meta(MetadataType.Title);

                if(title != null)
                    selectedTrackTitle.Text = title;
                else
                {
                    selectedTrackTitle.Text = playListTable[currentTrack].title;
                }
                if(artist != null)
                selectedTrackArtist.Text = artist;
                else
                {
                    artist = "";
                    selectedTrackArtist.Text  = artist;
                }
                
            
            }
            if (!token_created)
            {
                token = Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(1), UpdateTimer);
                token_created = true;
            }
        }

        private bool UpdateTimer(MainLoop arg)
        {


            if (mp.State != VLCState.Playing && currentTrack < playList.Count - 1 && 
            mp.State != VLCState.Paused && mp.State != VLCState.Stopped)
            {

                mp.SetPause(false);
                pauseButton.Text = "Pause";
                nextTrack();


            }
		    return true;
        }


        private void cleanUp()
        {
            string strCommand = "reset";
            System.Diagnostics.Process.Start(strCommand);
        }
    }
}
