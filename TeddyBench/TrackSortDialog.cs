using Id3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TeddyBench
{


    public partial class TrackSortDialog : Form
    {
        private string[] FileNames;
        private bool isAscending = false;
        private string[] originalColumnHeaders;
        private List<Tuple<string, Id3Tag>> FileList = new List<Tuple<string, Id3Tag>>();

        public TrackSortDialog()
        {
            InitializeComponent();
        }

        public TrackSortDialog(string[] fileNames) : this()
        {
            this.FileNames = fileNames;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var fileTuples = FileNames.Select(f => new Tuple<string, Id3Tag>(f, GetTag(f)));

            foreach (Tuple<string, Id3Tag> item in fileTuples.OrderBy(i => (i.Item2 == null) ? int.MaxValue : i.Item2.Track.Value))
            {
                FileList.Add(item);
            }

            UpdateView();       
        
        }


        private Id3Tag GetTag(string f)
        {
            try
            {
                Mp3 mp3 = new Mp3(f, Mp3Permissions.Read);

                //Id3Tag ret = mp3.GetAllTags().Where(t => t.Track.IsAssigned).FirstOrDefault();
                Id3Tag ret = mp3.GetAllTags().FirstOrDefault();

                return ret;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private void UpdateView()
        {
            lstTracks.Items.Clear();
            int track = 1;
            foreach (Tuple<string, Id3Tag> item in FileList)
            {
                ListViewItem lvi = new ListViewItem();

                lvi.Tag = item;
                lvi.Text = track.ToString();

                string id3 = "";
                string storedTrack = "";

                if (item.Item2 != null)
                {
                    string artist;
                    if (item.Item2.Artists == null)
                    {
                        artist = Regex.Replace(item.Item2.Band, @"\0+$", "");
                    } else
                    {
                        artist = Regex.Replace(item.Item2.Artists, @"\0+$", "");
                    }

                    id3 = artist + " - " + Regex.Replace(item.Item2.Title, @"\0+$", "");

                    if (item.Item2.Track.IsAssigned)
                    {
                        storedTrack = item.Item2.Track.ToString();
                    }

                }

                lvi.SubItems.Add(item.Item1);
                lvi.SubItems.Add(id3);
                lvi.SubItems.Add(item.Item2.Track);
                // Set the ToolTip text to the full ID3 information
                lvi.ToolTipText = id3;

                lstTracks.Items.Add(lvi);

                track++;
            }

            /* Resize each column to fit its contents. */
            for (int i = 0; i < lstTracks.Columns.Count; i++)
            {
                lstTracks.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
            }

            lstTracks.Columns[lstTracks.Columns.Count - 1].Width = -2;
        }

        public string[] SortedFiles
        {
            get
            {
                return FileList.Select(i => i.Item1).ToArray();
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            if (lstTracks.SelectedIndices.Count == 0)
            {
                return;
            }
            if (lstTracks.SelectedIndices[0] == 0)
            {
                return;
            }

            int numberOfItems = lstTracks.Items.Count;
            List<object> selectedItems = new List<object>();
            lstTracks.ListViewItemSorter = null;    // remove the sorting by column
            isAscending = false;

            foreach (var item in lstTracks.SelectedItems)
            {
                selectedItems.Add(item);
            }

            lstTracks.BeginUpdate();
            for (int i = 0; i < lstTracks.Items.Count; i++)
            {
                if (lstTracks.SelectedIndices.Contains(i))
                {
                    if (i > 0)
                    {
                        /* Check to avoid moving the first item further up */
                        ListViewItem item = lstTracks.Items[i];
                        lstTracks.Items.RemoveAt(i);
                        lstTracks.Items.Insert(i - 1, item);
                    }
                }
            }

            lstTracks.SelectedItems.Clear();
            lstTracks.EndUpdate();

            foreach (ListViewItem item in selectedItems)
            {
                item.Selected = true;
            }

            RebuildFileList();
            lstTracks.Select();
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if (lstTracks.SelectedIndices.Count == 0)
            {
                return;
            }

            if (lstTracks.SelectedIndices[lstTracks.SelectedIndices.Count-1] == lstTracks.Items.Count - 1)
            {
                return;
            }

            int numberOfItems = lstTracks.Items.Count;
            List<object> selectedItems = new List<object>();
            lstTracks.ListViewItemSorter = null;    // remove the sorting by column
            isAscending = false;

            foreach (var item in lstTracks.SelectedItems)
            {
                selectedItems.Add(item);
            }

            lstTracks.BeginUpdate();
            for (int i = numberOfItems - 2; i >= 0; i--)
            {
                if (lstTracks.SelectedIndices.Contains(i))
                {
                    ListViewItem item = lstTracks.Items[i];
                    lstTracks.Items.RemoveAt(i);
                    lstTracks.Items.Insert(i + 1, item);
                }
            }

            lstTracks.SelectedItems.Clear();
            lstTracks.EndUpdate();

            foreach (ListViewItem item in selectedItems)
            {
                item.Selected = true;
            }

            RebuildFileList();
            lstTracks.Select();
        }

        private void RebuildFileList()
        {
            FileList = new List<Tuple<string, Id3Tag>>();
            int track = 1;
            foreach (ListViewItem item in lstTracks.Items)
            {
                item.Text = track.ToString();
                track++;
                FileList.Add((Tuple<string, Id3Tag>)item.Tag);
            }
        }

        private void lstTracks_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column != 0) SortListView(e.Column);
        }

        private void SortListView(int columnIndex)
        {
            // Use ListViewItemComparer for sorting
            lstTracks.ListViewItemSorter = new ListViewItemComparer(columnIndex, !isAscending);
            lstTracks.Sort();
            isAscending = !isAscending;
            RebuildFileList();
        }


    }

    public class ListViewItemComparer : IComparer
    {
        private int columnIndex;
        private bool ascending;

        public ListViewItemComparer(int columnIndex, bool ascending)
        {
            this.columnIndex = columnIndex;
            this.ascending = ascending;
        }

        public int Compare(object x, object y)
        {
            ListViewItem item1 = (ListViewItem)x;
            ListViewItem item2 = (ListViewItem)y;

            // Get the values based on the specified column
            string value1 = item1.SubItems[columnIndex].Text;
            string value2 = item2.SubItems[columnIndex].Text;

            // Handle potential null values
            if (value1 == null && value2 == null)
            {
                return 0; // Both values are null, consider them equal
            }
            else if (value1 == null)
            {
                return ascending ? 1 : -1; // Null value comes after non-null in ascending order
            }
            else if (value2 == null)
            {
                return ascending ? -1 : 1; // Null value comes before non-null in descending order
            }

            // Compare strings based on chosen order
            int comparison = string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
            return ascending ? comparison : -comparison;
        }
    }

}
