using System;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace _2chMediaDownloader
{
	class Program
	{
		static readonly string[] MediaFileExtensions = "mp4;webm;jpeg;jpg;png;gif;bmp;webp".Split(";");

		static readonly Regex PostUriRegex =
			new Regex(@"2ch[.]hk/(.*?)/res/([0-9]*)[.]html");
		static readonly Regex MediaRegex =
			new Regex($"(data-src|src|href)=\"/(.*?)/([0-9]*)[.]({string.Join('|', MediaFileExtensions)})\"",
				RegexOptions.IgnorePatternWhitespace);


		static string URIsText;
		static bool SubFoldersRequired = false;

		static readonly DirectoryInfo SaveDirectoryInfo =
			new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\2chDownloads");


		[DllImport("kernel32")] static extern bool AllocConsole();

		[STAThread]
		public static void Main()
		{
			Label InfoLabel = new Label { Text = "Inset your 2ch.hk thread URIs here:" };
			RichTextBox URIsTextBox = new RichTextBox();
			CheckBox CreateSubFoldersCheckBox = new CheckBox { Text = "Create sub-folder for each thread", Checked = SubFoldersRequired };
			Button DownloadButton = new Button { Text = "Start downloading" };
			Form MainForm = new Form();

			URIsText = URIsTextBox.Text;

			URIsTextBox.TextChanged += URIsTextBox_TextChanged;
			CreateSubFoldersCheckBox.CheckedChanged += CreateSubFoldersCheckBox_CheckedChanged;

			Control[] AllControls = { InfoLabel, URIsTextBox, CreateSubFoldersCheckBox, DownloadButton };
			Boost.Controls.ToSameWidth(AllControls, 300);
			Panel AllElementsPanel = Boost.Controls.ToVerticalStackPanel(AllControls);
			MainForm.ClientSize = AllElementsPanel.Size;
			MainForm.Controls.Add(AllElementsPanel);

			DownloadButton.Click += DownloadButton_Click;

			Application.Run(MainForm);

		}

		public static void DownloadButton_Click(object sender, EventArgs e)
		{
			HtmlDocument CurrentHtmlDocument = new HtmlDocument();
			WebClient AutorizedWebClient = new WebClient();
			DirectoryInfo CurrentDirectory;
			string CurrentFileName;

			foreach (Uri ThreadUri in GetThreadURIsFromText(URIsText))
			{
				Trace.WriteLine($"Thread uri \"{ThreadUri.AbsoluteUri}\" recognized");

				Trace.WriteLine($"Trying to download thread at \"{ThreadUri.AbsoluteUri}\"");
				try
				{
					CurrentHtmlDocument.LoadHtml(AutorizedWebClient.DownloadString(ThreadUri));
				}
				catch (WebException we)
				{
					Trace.WriteLine($"Unable to download page \"{ThreadUri.AbsoluteUri}\" cause of \"{we.Message}\"");
					continue;
				}
				Trace.WriteLine($"Thread page \"{ThreadUri.AbsoluteUri}\" downloaded succesfully");

				if (SubFoldersRequired)
				{
					Trace.WriteLine($"Trying to create sub-folder for thread \"{ThreadUri.AbsoluteUri}\"");
					CurrentDirectory = SaveDirectoryInfo.CreateSubdirectory(GetThreadNumberFromURI(ThreadUri));
					Trace.WriteLine($"Sub-folder for media from thread \"{ThreadUri.AbsoluteUri}\" created as \"{CurrentDirectory.FullName}\"");
				}
				else
				{
					CurrentDirectory = SaveDirectoryInfo;
					Trace.WriteLine($"Sub-folder for thread \"{ThreadUri.AbsoluteUri}\" is not required. Files will be saved at \"{CurrentDirectory.FullName}\"");
				}

				foreach (Uri MediaUri in GetMediaURIsFromThreadHtmlPage(CurrentHtmlDocument))
				{
					Trace.WriteLine($"Media uri \"{MediaUri.AbsoluteUri}\" from thread \"{ThreadUri.AbsoluteUri}\" recognized");

					CurrentFileName = CurrentDirectory.FullName + '\\' + GetFileNameFromURI(MediaUri);

					if (File.Exists(CurrentFileName))
					{
						Trace.WriteLine(
							$"File \"{MediaUri.AbsoluteUri}\" from thread \"{ThreadUri.AbsoluteUri}\" skipped as already existing at \"{CurrentFileName}\"");
						continue;
					}

					Trace.WriteLine($"Trying to download media at \"{MediaUri.AbsoluteUri}\"");
					try
					{
						AutorizedWebClient.DownloadFile(MediaUri,
							CurrentDirectory.FullName + '\\' + GetFileNameFromURI(MediaUri));
					}
					catch (WebException we)
					{
						Trace.WriteLine($"Unable to download file \"{MediaUri.AbsoluteUri}\" from thread \"{ThreadUri.AbsoluteUri}\" cause of \"{we.Message}\"");
						continue;
					}
					Trace.WriteLine($"Media \"{MediaUri.AbsoluteUri}\" downloaded succesfully");
				}
			}
		}

		public static IEnumerable<Uri> GetMediaURIsFromThreadHtmlPage(HtmlDocument ThreadPage)
		{
			return
				MediaRegex.Matches(string.Concat(ThreadPage.DocumentNode.SelectNodes("//a[contains(@class, 'post__image-link')]").Select(x => x.OuterHtml)))
					.Where(x => !x.Value.Split('.')[0].EndsWith('s')).Select(x => new Uri(@"https://2ch.hk" + x.Value.Split('"')[1]));
		}

		public static IEnumerable<Uri> GetThreadURIsFromText(string Text)
		{
			return
				PostUriRegex.Matches(Text).Select(x => new Uri(@"https://" + x.Value));
		}

		public static string GetThreadNumberFromURI(Uri ThreadUri) => ThreadUri.AbsoluteUri.Split('/').Last().Split('.').First();

		public static string GetFileNameFromURI(Uri FileUri) => FileUri.AbsoluteUri.Split('/').Last();


		public static void CreateSubFoldersCheckBox_CheckedChanged(object sender, EventArgs e) => SubFoldersRequired = ((CheckBox)sender).Checked;
		public static void URIsTextBox_TextChanged(object sender, EventArgs e) => URIsText = ((RichTextBox)sender).Text;

	}
}

namespace Boost
{
	public static class Controls
	{
		public static Panel ToVerticalStackPanel(IList<Control> Controls, int space = 0)
		{
			Panel Out = new Panel();
			int CurrentHeight = 0;
			int OutWid = 0;

			for (int i = 0; i < Controls.Count; i++)
			{
				Controls[i].Location = new Point(0, CurrentHeight);
				Out.Controls.Add(Controls[i]);
				CurrentHeight += Controls[i].Height + space;
				if (Controls[i].Width > OutWid) OutWid = Controls[i].Width;
			}

			Out.Height = CurrentHeight - space;
			Out.Width = OutWid;
			return Out;
		}

		public static void ToSameWidth(IList<Control> Controls, int Width)
		{
			for (int i = 0; i < Controls.Count; i++) Controls[i].Width = Width;
		}
	}
}
