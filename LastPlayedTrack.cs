using System;
using System.Collections.Generic;
using System.IO;


namespace CLIMusicDotNet
{
    class Track
    {
        public string title;
        public string directory;
    }   

    class LastPlayedTrack
    {
        int position;
        long time_pos;
        public bool file_exists = false;
        public bool playlist_exists = false;
        string current_dir;
        List<string> saveInfo = new List<string>();
        List<Track> fileTable = new List<Track>();
        string savefile = "save.pl";
        string playlistfile = "playlist.pl";  


        public void write(List<Track> pl, double time_pos, int current_track, string current_dir)
        {

            
            System.IO.File.Create(playlistfile).Close();
            var sw = System.IO.File.AppendText(playlistfile);
            
            foreach(Track t in pl)
            {
                sw.WriteLine(t.title + "|" + t.directory);
            }

            sw.Close();

            

            this.saveInfo.Clear();
            this.position = current_track;
            this.saveInfo.Add(current_dir);
            this.saveInfo.Add(time_pos.ToString());
            this.saveInfo.Add(position.ToString());
            System.IO.File.Create(savefile).Close();
            System.IO.File.WriteAllLines(savefile, this.saveInfo);
            
        }

        public void load()
        {

            if(File.Exists(playlistfile))
            {

                playlist_exists = true;
                var my_playlist = File.ReadAllLines(playlistfile);
                foreach(string line in my_playlist)
                {
                    var tokens = line.Split("|");
                    var k = tokens[0];
                    var value = tokens[1];

                    var track = new Track();
                    track.title = k;
                    track.directory = value;
                    fileTable.Add(track);
                    
                }
                if(System.IO.File.Exists(savefile))
                {
                    saveInfo.Clear();
                    var trackPositions = File.ReadAllLines(savefile);
                    file_exists = true;
                    this.position = Convert.ToInt16(trackPositions[2]); 
                    this.time_pos = (long)Convert.ToInt64(trackPositions[1]);
                    this.current_dir = trackPositions[0].ToString();
                }
            }
        }


        public long getTrackTime()
        {
            return this.time_pos;
        }

        public int getPlaylistPosition()
        {
            return this.position;
        }

        public string getLastDirectory()
        {
            return this.current_dir;
        }

        public List<Track> getPlayListFiles()
        {
            return fileTable;
        }

    }

}