using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace TkTv
{
    public partial class Form1 : Form
    {
        public bool Updating = false;
        public XmlDocument XmlFile;
        public string XmlFileName = "config.xml";
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            CreateConfigFileIfNotExists();
            LoadOptions();
            LoadTvData();
        }
        public void CreateConfigFileIfNotExists()
        {
            if (!File.Exists(XmlFileName))
            {
                XmlFile = new XmlDocument();
                XmlNode XmlTmpRoot, XmlTmpFilters;
                XmlTmpRoot = XmlFile.CreateElement("tktv");
                XmlTmpFilters = XmlFile.CreateElement("filters");
                XmlAttribute XmlTmp = XmlFile.CreateAttribute("type");
                XmlTmp.InnerText = "normal";
                XmlTmpFilters.Attributes.Append(XmlTmp);
                XmlTmp = XmlFile.CreateAttribute("normal");
                XmlTmp.InnerText = "";
                XmlTmpFilters.Attributes.Append(XmlTmp);
                XmlTmp = XmlFile.CreateAttribute("show");
                XmlTmp.InnerText = "0";
                XmlTmpFilters.Attributes.Append(XmlTmp);
                XmlTmpRoot.AppendChild(XmlTmpFilters);
                XmlFile.AppendChild(XmlTmpRoot);
                XmlFile.Save(XmlFileName);
            }
            else
            {
                XmlFile = new XmlDocument();
                XmlFile.Load(XmlFileName);
            }
        }
        public bool IsFiltered(string Channel)
        {
            if (!checkBox1.Checked && comboBox2.SelectedIndex == 0)
            {
                return false;
            }
            if (!checkBox1.Checked && comboBox2.SelectedItem.ToString() == Channel)
            {
                return false;
            }
            if (checkBox1.Checked)
            {
                if (listBox1.SelectedItems.Count > 0)
                {
                    for (int i = 0; i < listBox1.SelectedItems.Count; i++)
                    {
                        if (listBox1.SelectedItems[i].ToString() == Channel)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    XmlNodeList XmlRef = XmlFile.SelectNodes("/tktv/filters/filter");
                    foreach (XmlNode Filter in XmlRef)
                    {
                        if (Filter.InnerText.ToString() == Channel)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        public void LoadOptions()
        {
            XmlNode XmlTmp = XmlFile.SelectSingleNode("/tktv/filters");
            if (XmlTmp.Attributes["type"].InnerText == "normal")
            {
                label3.Enabled = true;
                comboBox2.Enabled = true;
            }
            else
            {
                label3.Enabled = false;
                comboBox2.Enabled = false;
                checkBox1.Checked = true;
            }
            comboBox1.SelectedIndex = Convert.ToInt16(XmlTmp.Attributes["show"].InnerText.ToString());
        }
        public void LoadFilters()
        {
            XmlNodeList XmlRef = XmlFile.SelectNodes("/tktv/filters/filter");
            foreach (XmlNode Filter in XmlRef)
            {
                for (int i = 0; i < listBox1.Items.Count; i++)
                {
                    if (listBox1.Items[i].ToString() == Filter.InnerText)
                    {
                        listBox1.SetSelected(i, true);
                    }
                }
            }
            XmlNode XmlRef2 = XmlFile.SelectSingleNode("/tktv/filters");
            for (int i = 0; i < comboBox2.Items.Count; i++)
                {
                    if (comboBox2.Items[i].ToString() == XmlRef2.Attributes["normal"].InnerText)
                    {
                        comboBox2.SelectedIndex = i;
                        break;
                    }
                }
        }
        public void LoadTvData()
        {
            Thread TvThread = new Thread(new ThreadStart(LoadTvDataThread));
            TvThread.Start();
        }
        public void LoadTvDataThread()
        {
            if (!Updating)
            {
                if (comboBox2.Items.Count == 1)
                {
                    listBox1.Enabled = false;
                    comboBox2.Enabled = false;
                }
                Updating = true;
                listView1.Items.Clear();
                Text = "TkTv - Aktualisiere...";
                string url = "";
                if (comboBox1.SelectedIndex == 0)
                {
                    url = "http://www.tvmovie.de/rss/tvjetzt.xml";
                }
                else if (comboBox1.SelectedIndex == 1)
                {
                    url = "http://www.tvmovie.de/rss/tv2015.xml";
                }
                else if (comboBox1.SelectedIndex == 2)
                {
                    url = "http://www.tvmovie.de/rss/tv2200.xml";
                }
                HttpWebRequest TvStream = (HttpWebRequest)WebRequest.Create(url);
                TvStream.Method = "GET";
                TvStream.AllowAutoRedirect = false;
                HttpWebResponse TvResponse = (HttpWebResponse)TvStream.GetResponse();
                Stream TvResponseData = TvResponse.GetResponseStream();
                StreamReader TvReader = new StreamReader(TvResponseData);
                String TvXmlResponse = TvReader.ReadToEnd();
                XmlDocument TvXml = new XmlDocument();
                TvXml.LoadXml(TvXmlResponse);
                Regex Pattern;
                Match Result;
                XmlNodeList XmlRef = TvXml.SelectNodes("/rss/channel/item/title");
                bool FillFilter = false;
                if (comboBox2.Items.Count == 1)
                {
                    FillFilter = true;
                }
                foreach (XmlNode Item in XmlRef)
                {
                    Pattern = new Regex("^([0-9]{1,}:[0-9]{1,}) (.*?) - (.*?)$", RegexOptions.Singleline);
                    Result = Pattern.Match(Item.InnerText.ToString());
                    if (!IsFiltered(Result.Groups[2].ToString()))
                    {
                        listView1.Items.Add(new ListViewItem(new string[] { Result.Groups[1].ToString(), Result.Groups[2].ToString(), Result.Groups[3].ToString() }));
                    }
                    if (FillFilter)
                    {
                        comboBox2.Items.Add(Result.Groups[2].ToString());
                        listBox1.Items.Add(Result.Groups[2].ToString());
                    }
                }
                Text = "TkTv";
                Updating = false;
                LoadFilters();
                if (FillFilter)
                {
                    if (!checkBox1.Checked)
                    {
                        comboBox2.Enabled = true;
                    }
                    else
                    {
                        listBox1.Enabled = true;
                    }
                }
            }
        }
        public void Save()
        {
            XmlDocument XmlFile = new XmlDocument();
            XmlFile.Load(XmlFileName);
            XmlNodeList XmlRef = XmlFile.SelectNodes("/tktv/filters/filter");
            foreach (XmlNode Filter in XmlRef)
            {
                Filter.ParentNode.RemoveChild(Filter);
            }
            XmlNode XmlRef2 = XmlFile.SelectSingleNode("/tktv/filters");
            foreach (string filter in listBox1.SelectedItems)
            {
                XmlNode XmlTmp = XmlFile.CreateElement("filter");
                XmlTmp.InnerText = filter;
                XmlRef2.AppendChild(XmlTmp);
            }


            if (checkBox1.Checked)
            {
                XmlRef2.Attributes["type"].InnerText = "extended";
            }
            else
            {
                XmlRef2.Attributes["type"].InnerText = "normal";
            }
            XmlRef2.Attributes["normal"].InnerText = comboBox2.SelectedItem.ToString();
            XmlRef2.Attributes["show"].InnerText = comboBox1.SelectedIndex.ToString();
            XmlFile.Save(XmlFileName);
        }
        private void aktualisierenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadTvData();
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTvData();
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTvData();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            LoadTvData();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            LoadTvData();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (panel3.Visible)
            {
                panel3.Visible = false;
                button2.Text = "Erweiterten Senderfilter anzeigen";
            }
            else
            {
                panel3.Visible = true;
                button2.Text = "Erweiterten Senderfilter verbergen";
            }
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                label3.Enabled = false;
                comboBox2.Enabled = false;
                listBox1.Enabled = true;
            }
            else
            {
                label3.Enabled = true;
                comboBox2.Enabled = true;
                listBox1.Enabled = false;
            }
            LoadTvData();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            Save();
        }
        private void einstellungenSpeichernToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }
        private void listBox1_Click(object sender, EventArgs e)
        {
            LoadTvData();
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://thomaskekeisen.de");
        }
    }
}
