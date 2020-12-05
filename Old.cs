//using System;
//using System.Net;
//using System.IO;
//using System.Text.RegularExpressions;
//using System.Linq;
//using System.Collections.Generic;
//using System.Threading;
//using System.Diagnostics;
//using System.Windows.Forms;
//using System.Drawing;
//using System.Diagnostics.Tracing;
//using System.Runtime.InteropServices;
//using HtmlAgilityPack;

//namespace _2chMediaDownloader
//{
//	class Old
//	{
//		static Form MainForm;
//		static Label InputLinksLabel;
//		static RichTextBox InputLinksTextBox;
//		static Label InputFileExtensionsLabel;
//		static TextBox InputFileExtensionsTextBox;
//		static CheckBox CreateSubFoldersCheckBox;
//		static Button StartDownloadButton;

//		static readonly string[] MediaFileExtensions = "mp4;webm;jpeg;jpg;png;gif;bmp;webp".Split(";");
//		static readonly DirectoryInfo MainDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\2chDownloads");
//		static readonly string _2chDomain = @"https://2ch.hk";

//		static readonly Regex MediaRegex = new Regex($"(data-src|src)=\"(.*?)([0-9]*)[.]({string.Join('|', MediaFileExtensions)})\"", RegexOptions.IgnorePatternWhitespace);
//		static readonly Regex PostUriRegex = new Regex(@"(.*?)2ch[.]hk/(.*?)/res/([0-9]*)[.]html");

//		static readonly WebClient MainWebClient = new WebClient();

//		static IList<ThreadMedia> AllThreadsMedias;
//		static int CurrentMediaId;
//		static int NowDownloadingCounter;
//		static readonly int MaxParallelDownloads = 3;

//		[DllImport("kernel32.dll")] static extern bool AllocConsole();

//		public static void Main1()
//		{
//#if DEBUG
//			AllocConsole();
//#endif
//			Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
//			Trace.WriteLine("Console out OK");
//			MainWebClient.Headers.Add(HttpRequestHeader.Cookie, @"usercode_auth=e1bade2d422b2e6a01b0bd04f82fd43b; plashque=1");
//			FillMainForm();
//			StartDownloadButton.Click += StartDownloadButton_Click;
//			Application.Run(MainForm);

//		}
//		public static void FillMainForm()
//		{
//			MainForm = new Form { Size = new Size(300, 400) };

//			InputLinksLabel =
//				new Label { Text = "Insert 2ch-Thread links:" };
//			InputLinksTextBox =
//				new RichTextBox();
//			InputFileExtensionsLabel =
//				new Label { Text = "Insert allowed file extensions: " };
//			InputFileExtensionsTextBox =
//				new TextBox { Text = "mp4;webm;jpeg;jpg;png;gif;bmp;webp" };
//			CreateSubFoldersCheckBox =
//				new CheckBox { Text = "Create sub-folders for each thread" };
//			StartDownloadButton =
//				new Button { Text = "Start downloading" };

//			Control[] AllControls = new Control[] {
//				InputLinksLabel,
//				InputLinksTextBox,
//				InputFileExtensionsLabel,
//				InputFileExtensionsTextBox,
//				CreateSubFoldersCheckBox,
//				StartDownloadButton
//			};
//			Boost.Controls.ToSameWidth(AllControls, MainForm.ClientSize.Width);
//			MainForm.Controls.Add(Boost.Controls.ToVerticalStackPanel(AllControls, 0));
//			MainForm.ClientSize = new Size(MainForm.ClientSize.Width, MainForm.Controls[0].Height);
//		}

//		public static void StartDownloadButton_Click(object sender, EventArgs e)
//		{
//			foreach (Control ctr in MainForm.Controls) ctr.Enabled = false;
//			Trace.WriteLine("\nDownload button click recongnized");
//			List<(Uri Thread, IEnumerable<Uri> Media)> ThreadMedias = new List<(Uri Thread, IEnumerable<Uri> Media)>();

//			foreach (Uri ThreadUri in InputLinksTextBox.Text.Split(",; \n".ToCharArray()).Where(x => PostUriRegex.IsMatch(x)).Select(x => new Uri(x)))
//			{
//				Trace.WriteLine($"\nThread uri \"{ThreadUri}\" recognized");
//				string HtmlString;
//				if (!TryDownloadPage(ThreadUri, out HtmlString)) continue;

//				ThreadMedias.Add((ThreadUri, HtmlStringGetMediaUris(HtmlString)));
//			}
//			if (ThreadMedias.Count() == 0 || ThreadMedias.Select(x => x.Media.Count()).Sum() < 1) return;
//			Trace.WriteLine("All media Uris founded:");
//			Trace.WriteLine(string.Join('\n',
//				ThreadMedias.Select(x => $"From thread \"{x.Thread.AbsoluteUri}\" recognized media URIs: "
//				+ string.Join('\n', x.Media.Select(y => '"' + y.AbsoluteUri + '"')))));


//			MainDirectory.Create();
//			foreach (var thread_medias in ThreadMedias)
//			{
//				DirectoryInfo CurrentDirectory;
//				if (CreateSubFoldersCheckBox.Checked) CurrentDirectory = MainDirectory.CreateSubdirectory(thread_medias.Thread.AbsoluteUri.Split('/').Last());
//				else CurrentDirectory = MainDirectory;

//				foreach (Uri MediaUri in thread_medias.Media)
//				{
//					TryDownloadFile(MediaUri, new FileInfo(CurrentDirectory.FullName + '\\' + MediaUri.AbsoluteUri.Split('/').Last()));
//				}
//			}

//			foreach (Control ctr in MainForm.Controls) ctr.Enabled = true;
//		}


//		public static bool ThreadUriIsValid(Uri ThreadUri) => ThreadUriIsValid(ThreadUri.AbsoluteUri);
//		public static bool ThreadUriIsValid(string ThreadUri)
//		{
//			return PostUriRegex.IsMatch(ThreadUri);
//		}

//		public static bool TryDownloadFile(Uri FileUri, FileInfo FileToSaveIn, WebClient wc = null)
//		{
//			if (wc == null) wc = MainWebClient;

//			if (!FileToSaveIn.Directory.Exists) FileToSaveIn.Directory.Create();
//			if (FileToSaveIn.Exists) { Trace.WriteLine($"\"{FileUri}\" skipped as already existing"); return true; }

//			try
//			{
//				wc.DownloadFile(FileUri, FileToSaveIn.FullName);
//			}
//			catch (WebException we)
//			{
//				Trace.WriteLine($"Unable to download file \"{FileUri}\" cause of \"{we.Message}\"");
//				return false;
//			}

//			return true;
//		}
//		public static bool TryDownloadPage(Uri PageUri, out string HtmlString, WebClient wc = null)
//		{
//			if (wc == null) wc = MainWebClient;

//			try
//			{
//				HtmlString = wc.DownloadString(PageUri);
//			}
//			catch (WebException we)
//			{
//				Trace.WriteLine($"Unable to download page \"{PageUri}\" cause of \"{we.Message}\"");
//				HtmlString = null; return false;
//			}

//			return true;
//		}
//		public static IEnumerable<Uri> HtmlStringGetMediaUris(string HtmlString)
//		{
//			return MediaRegex.Matches(HtmlString).Where(x => !x.Value.Contains("banner")).Select(x => new Uri(_2chDomain + x.Value.Split('"')[1])).Where(x => !x.AbsoluteUri.Split('/').Last().Split('.').First().EndsWith('s'));
//		}



//		struct ThreadMedia
//		{
//			public Uri ThreadUri;
//			public Uri MediaUri;
//			public ThreadMedia(Uri ThreadUri, Uri MediaUri)
//			{
//				this.ThreadUri = ThreadUri;
//				this.MediaUri = MediaUri;
//			}
//		}
//	}
//}
