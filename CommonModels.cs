using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using WDCMLSDKBase;
using WDCMLSDKDerived;

namespace WDCMLSDK
{
	/// <summary>
	/// Represents the topic type for an API ref topic.
	/// </summary>
	internal enum TopicType
	{
		NotYetKnown,
		AttachedPropertyWinRT,
		AttributeWinRT,
		ClassWinRT,
		DelegateWinRT,
		EnumWinRT,
		EventWinRT,
		InterfaceWinRT,
		MethodWinRT,
		NamespaceWinRT,
		PropertyWinRT,
		StructWinRT,
	}

	/// <summary>
	/// A hierarchical object model representing a set of API ref docs (for a specified DocSet).
	/// </summary>
	internal class ApiRefModel
	{
		public List<NamespaceWinRT> NamespaceWinRTs = new List<NamespaceWinRT>();

		public NamespaceWinRT EnsureNamespaceWinRT(string projectName)
		{
			NamespaceWinRT namespaceWinRT = null;
			foreach (NamespaceWinRT eachNamespaceWinRT in this.NamespaceWinRTs)
			{
				if (eachNamespaceWinRT.ProjectName == projectName)
				{
					namespaceWinRT = eachNamespaceWinRT;
					break;
				}
			}
			if (namespaceWinRT == null)
			{
				namespaceWinRT = new NamespaceWinRT() { ProjectName = projectName };
				this.NamespaceWinRTs.Add(namespaceWinRT);
			}
			return namespaceWinRT;
		}

		public string GetProjectNameForNamespace(string namespaceName)
		{
			foreach (NamespaceWinRT eachNamespaceWinRT in this.NamespaceWinRTs)
			{
				if (eachNamespaceWinRT.Name == namespaceName)
				{
					return eachNamespaceWinRT.ProjectName;
				}
			}
			return null;
		}

		public bool FindClassWinRTInApiRefModel(string namespaceName, ClassWinRT classWinRT)
		{
			foreach (NamespaceWinRT eachNamespaceWinRT in this.NamespaceWinRTs)
			{
				if (eachNamespaceWinRT.Name == namespaceName)
				{
					foreach (ClassWinRT eachClassWinRT in eachNamespaceWinRT.ClassWinRTs)
					{
						if (classWinRT.Name == eachClassWinRT.Name)
						{
							return true;
						}
						else if (eachClassWinRT.InterfacesImplemented != null)
						{
							foreach (string eachInterfaceImplemented in eachClassWinRT.InterfacesImplemented)
							{
								if (eachInterfaceImplemented == classWinRT.Name)
								{
									return true;
								}
							}
						}
					}
				}
			}
			return false;
		}

		public ClassWinRT GetClassWinRTInApiRefModel(string namespaceName, ClassWinRT classWinRT)
		{
			foreach (NamespaceWinRT eachNamespaceWinRT in this.NamespaceWinRTs)
			{
				if (eachNamespaceWinRT.Name == namespaceName)
				{
					foreach (ClassWinRT eachClassWinRT in eachNamespaceWinRT.ClassWinRTs)
					{
						if (classWinRT.Name == eachClassWinRT.Name)
						{
							return eachClassWinRT;
						}
					}
				}
			}
			return null;
		}
	}

	/// <summary>
	/// A class representing a WinRT/UWP namespace.
	/// </summary>
	internal class NamespaceWinRT
	{
		public List<ClassWinRT> ClassWinRTs = new List<ClassWinRT>();

		public string Name = string.Empty;
		public string ProjectName = null;

		public static void ProcessNamespaceWinRT(string projectName, List<Editor> topicEditors, ref ApiRefModel apiRefModel)
		{
			string namespaceName = string.Empty;
			// keep a list of the NamespaceWinRTs we create so we can set their name at the end.
			List<NamespaceWinRT> namespaceWinRTsWeCreated = new List<NamespaceWinRT>();

			foreach (Editor eachTopicEditor in topicEditors)
			{
				// determine the type of the topic.
				TopicType topicType = eachTopicEditor.GetMetadataAtTypeAsEnum();

				if (topicType == TopicType.NamespaceWinRT)
				{
					namespaceName = eachTopicEditor.GetMetadataAtTitle();
					continue;
				}

				NamespaceWinRT namespaceWinRT = apiRefModel.EnsureNamespaceWinRT(projectName);
				if (!namespaceWinRTsWeCreated.Contains(namespaceWinRT)) namespaceWinRTsWeCreated.Add(namespaceWinRT);
				namespaceWinRT.ProcessOneTopic(topicType, eachTopicEditor);
			}

			foreach (NamespaceWinRT eachNamespaceWinRT in namespaceWinRTsWeCreated)
			{
				eachNamespaceWinRT.Name = namespaceName;
			}
		}

		private void ProcessOneTopic(TopicType topicType, Editor editor)
		{
			ClassWinRT classWinRT = null;
			switch (topicType)
			{
				case TopicType.AttachedPropertyWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetAppliesClassOrIfaceXrefAtRid(), string.Empty, null, editor.FileInfo);
					classWinRT.AddMember(editor.GetMetadataAtId(), editor.GetMetadataAtTitle(), editor.GetMetadataAtIntellisenseIdString(), topicType, editor.FileInfo);
					break;
				case TopicType.AttributeWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetMetadataAtId(), editor.GetTypeNameFromMetadataAtIntellisenseIdString(), editor.GetInterfacesImplemented(), editor.FileInfo, TopicType.AttributeWinRT);
					break;
				case TopicType.ClassWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetMetadataAtId(), editor.GetTypeNameFromMetadataAtIntellisenseIdString(), editor.GetInterfacesImplemented(), editor.FileInfo, TopicType.ClassWinRT);
					break;
				case TopicType.DelegateWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetMetadataAtId(), editor.GetTypeNameFromMetadataAtIntellisenseIdString(), editor.GetInterfacesImplemented(), editor.FileInfo, TopicType.DelegateWinRT);
					break;
				case TopicType.EnumWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetMetadataAtId(), editor.GetTypeNameFromMetadataAtIntellisenseIdString(), editor.GetInterfacesImplemented(), editor.FileInfo, TopicType.EnumWinRT);
					break;
				case TopicType.EventWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetAppliesClassOrIfaceXrefAtRid(), string.Empty, null, editor.FileInfo);
					classWinRT.AddMember(editor.GetMetadataAtId(), editor.GetMetadataAtTitle(), editor.GetMetadataAtIntellisenseIdString(), topicType, editor.FileInfo);
					break;
				case TopicType.InterfaceWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetMetadataAtId(), editor.GetTypeNameFromMetadataAtIntellisenseIdString(), editor.GetInterfacesImplemented(), editor.FileInfo, TopicType.InterfaceWinRT);
					break;
				case TopicType.MethodWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetAppliesClassOrIfaceXrefAtRid(), string.Empty, null, editor.FileInfo);
					classWinRT.AddMember(editor.GetMetadataAtId(), editor.GetMetadataAtTitle() + editor.GetMethodWinRTParametersFromParams(), editor.GetMetadataAtIntellisenseIdString(), topicType, editor.FileInfo);
					break;
				case TopicType.PropertyWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetAppliesClassOrIfaceXrefAtRid(), string.Empty, null, editor.FileInfo);
					classWinRT.AddMember(editor.GetMetadataAtId(), editor.GetMetadataAtTitle(), editor.GetMetadataAtIntellisenseIdString(), topicType, editor.FileInfo);
					break;
				case TopicType.StructWinRT:
					classWinRT = this.EnsureClassWinRT(editor.GetMetadataAtId(), editor.GetTypeNameFromMetadataAtIntellisenseIdString(), editor.GetInterfacesImplemented(), editor.FileInfo, TopicType.StructWinRT);
					break;
			}
		}

        public ClassWinRT EnsureClassWinRT(string id, string name, List<string> interfacesImplemented, FileInfo fileInfo, TopicType topicType = TopicType.NotYetKnown, ClassWinRTProvenance classWinRTProvenance = ClassWinRTProvenance.Topic)
		{
			ClassWinRT classWinRT = null;
			foreach (ClassWinRT eachClassWinRT in this.ClassWinRTs)
			{
				if ((id != string.Empty && eachClassWinRT.Id == id) || (name != string.Empty && eachClassWinRT.Name == name))
				{
					classWinRT = eachClassWinRT;
					if (classWinRT.Id == string.Empty) classWinRT.Id = id;
					if (classWinRT.Name == string.Empty) classWinRT.Name = name;
					if (classWinRT.TopicType == TopicType.NotYetKnown) classWinRT.TopicType = topicType;
					break;
				}
			}
			if (classWinRT == null)
			{
				classWinRT = new ClassWinRT(id, name, fileInfo, classWinRTProvenance);
				if (topicType != TopicType.NotYetKnown) classWinRT.TopicType = topicType;
				this.ClassWinRTs.Add(classWinRT);
			}

			if (interfacesImplemented != null) classWinRT.InterfacesImplemented = interfacesImplemented;

			return classWinRT;
		}
	}

	/// <summary>
	/// Indicates whether a class was input from a topic or from a config file.
	/// </summary>
	internal enum ClassWinRTProvenance
	{
		Topic,
		ConfigFile
	}

	/// <summary>
	/// A class representing a WinRT/UWP class.
	/// </summary>
	internal class ClassWinRT
	{
		public ClassWinRTProvenance ClassWinRTProvenance;

		public ClassWinRT(string id, string name, FileInfo fileInfo, ClassWinRTProvenance classWinRTProvenance)
		{
			this.Id = id;
			this.Name = name;
			this.FileInfo = fileInfo;
			this.ClassWinRTProvenance = classWinRTProvenance;
		}

		public string Id = string.Empty;
		public string Name = string.Empty;
		public TopicType TopicType = TopicType.NotYetKnown;
		public FileInfo FileInfo;
		public List<string> InterfacesImplemented;
		public List<MemberWinRT> MemberWinRTs = new List<MemberWinRT>();

		public void AddMember(string id, string name, string intellisense_id_string, TopicType topicType, FileInfo fileInfo)
		{
			MemberWinRT memberWinRT = new MemberWinRT(id, name, intellisense_id_string, topicType, fileInfo);
			this.MemberWinRTs.Add(memberWinRT);
		}

		public bool HasMemberWinRTs
		{
			get
			{
				return (this.TopicType == TopicType.EnumWinRT || this.TopicType == TopicType.StructWinRT || MemberWinRTs.Count != 0);
			}
		}

		public string DisplayName
		{
			get
			{
				// Remove any `<n> generics crap from the end.
				int startIndexOfWeirdApostrophe = this.Name.IndexOf('`');
				if (startIndexOfWeirdApostrophe != -1)
				{
					return this.Name.Substring(0, startIndexOfWeirdApostrophe);
				}
				return this.Name;
			}
		}

		public bool GetMemberWinRTByName(string memberName, ref MemberWinRT foundMemberWinRT)
		{
			foreach (MemberWinRT eachMemberWinRT in this.MemberWinRTs)
			{
				if (eachMemberWinRT.Name == memberName)
				{
					foundMemberWinRT = eachMemberWinRT;
					return true;
				}
			}
			return false;
		}

		public bool GetMemberWinRTByIntellisenseId(string intellisense_id_string, ref MemberWinRT foundMemberWinRT)
		{
			foreach (MemberWinRT eachMemberWinRT in this.MemberWinRTs)
			{
				if (eachMemberWinRT.IntellisenseId == intellisense_id_string)
				{
					foundMemberWinRT = eachMemberWinRT;
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// A class representing a member of a WinRT/UWP class, struct, interface, or attribute.
	/// </summary>
	internal class MemberWinRT
	{
		public MemberWinRT(string id, string name, string intellisense_id_string, TopicType topicType, FileInfo fileInfo)
		{
			this.Id = id;
			this.Name = name;
			this.IntellisenseId = intellisense_id_string;
			this.TopicType = topicType;
			this.FileInfo = fileInfo;
		}

		public string Id = string.Empty;
		public string Name = string.Empty;
		public string IntellisenseId = string.Empty;
		public TopicType TopicType = TopicType.NotYetKnown;
		public FileInfo FileInfo;
	}

	/// <summary>
	/// Utility class used for sorting NamespaceWinRTs.
	/// </summary>
	internal class NamespaceWinRTComparer : Comparer<NamespaceWinRT>
	{
		public override int Compare(NamespaceWinRT lhs, NamespaceWinRT rhs)
		{
			return lhs.Name.CompareTo(rhs.Name);
		}
	}

	/// <summary>
	/// Utility class used for sorting ClassWinRTs.
	/// </summary>
	internal class ClassWinRTComparer : Comparer<ClassWinRT>
	{
		public override int Compare(ClassWinRT lhs, ClassWinRT rhs)
		{
			return lhs.Name.CompareTo(rhs.Name);
		}
	}

	/// <summary>
	/// Utility class used for sorting MemberWinRTs.
	/// </summary>
	internal class MemberWinRTComparer : Comparer<MemberWinRT>
	{
		public override int Compare(MemberWinRT lhs, MemberWinRT rhs)
		{
			return lhs.Name.CompareTo(rhs.Name);
		}
	}

	/// <summary>
	/// A class representing project ownership.
	/// </summary>
	internal static class ProjectOwners
	{
		public static Dictionary<string, string> LoadUniqueKeyMap()
		{
			try
			{
				ProgramBase.ConsoleWrite("Loading project ownership data... ", ConsoleWriteStyle.Default, false);
				string sqlcmdArgs = "-W -X -s\",\" -S 10.185.184.7 -Q \"USE DDCM" +
					"S_WS_DEV SELECT dbo.Reporting_Topic.[Project Name], dbo.Reporting_Topic.Writer FROM dbo.Reporting_Topic WHERE dbo.Reporting_Topic.[Team Name]='_XmetaLProj" +
					"ects' AND dbo.Reporting_Topic.Title LIKE '$$%%' ORDER BY dbo.Reporting_Topic.[Project Name]\"";

				var info = new ProcessStartInfo("sqlcmd", sqlcmdArgs);
				info.WorkingDirectory = ProgramBase.ExeFolderPath;
				info.UseShellExecute = false;
				info.CreateNoWindow = true;
				info.RedirectStandardInput = true;
				info.RedirectStandardOutput = true;

				using (Process exeProcess = Process.Start(info))
				{
					exeProcess.StandardInput.WriteLine("sqlcmd " + sqlcmdArgs);
					string projectOwnersList = exeProcess.StandardOutput.ReadToEnd();
					exeProcess.WaitForExit();

					Dictionary<string, string> mappingsDict = new Dictionary<string, string>();

					using (StreamReader streamReader = GenerateStreamReaderFromString(projectOwnersList))
					{
						string currentLine = null;

						while (null != (currentLine = streamReader.ReadLine()))
						{
							string[] values = currentLine.Split(',');

							for (int ix = 0; ix < values.Length; ++ix)
							{
								values[ix] = values[ix].Trim();
							}

							if (values.Length == 2 && values[0].Length > 0 && values[1].Length > 0)
							{
								mappingsDict[values[0]] = values[1];
							}
						}
					}
					Directory.SetCurrentDirectory(ProgramBase.EnlistmentDirectoryInfo.FullName);
					ProgramBase.ConsoleWrite("SUCCEEDED.", ConsoleWriteStyle.Success);
					ProgramBase.ConsoleWrite(string.Empty);
					return mappingsDict;
				}
			}
			catch (Exception ex)
			{
				ProgramBase.ConsoleWrite("FAILED.", ConsoleWriteStyle.Error);
				ProgramBase.ConsoleWrite(ex.Message, ConsoleWriteStyle.Error);
				ProgramBase.ConsoleWrite(string.Empty);
				throw new WDCMLSDKException();
			}
		}

		private static StreamReader GenerateStreamReaderFromString(string s)
		{
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return new StreamReader(stream);
		}
	}

	//internal class ProjectOwners
	//{
	//	public ProjectOwners()
	//	{
	//		try
	//		{
	//			ProgramBase.ConsoleWrite("Loading project ownership data... ", ConsoleWriteStyle.Default, false);
	//			string tempFile = "ProjectOwnersGenerated.txt";
	//			string sqlcmd = "sqlcmd -W -X -s\",\" -S 10.185.184.7 -Q \"USE DDCM" +
	//				"S_WS_DEV SELECT dbo.Reporting_Topic.[Project Name], dbo.Reporting_Topic.Writer FROM dbo.Reporting_Topic WHERE dbo.Reporting_Topic.[Team Name]='_XmetaLProj" +
	//				"ects' AND dbo.Reporting_Topic.Title LIKE '$$%%' ORDER BY dbo.Reporting_Topic.[Project Name]\" " +
	//				"> " + tempFile;

	//			var info = new ProcessStartInfo("cmd");
	//			info.WorkingDirectory = ProgramBase.ExeFolderPath;
	//			info.UseShellExecute = false;
	//			info.CreateNoWindow = false;
	//			info.WindowStyle = ProcessWindowStyle.Normal;
	//			info.RedirectStandardInput = true;

	//			using (Process exeProcess = Process.Start(info))
	//			{
	//				exeProcess.StandardInput.WriteLine(sqlcmd);
	//				exeProcess.WaitForExit();
	//			}

	//			ProgramBase.ConsoleWrite("SUCCEEDED.", ConsoleWriteStyle.Success);
	//			ProgramBase.ConsoleWrite(string.Empty);
	//		}
	//		catch (Exception ex)
	//		{
	//			ProgramBase.ConsoleWrite("FAILED.", ConsoleWriteStyle.Error);
	//			ProgramBase.ConsoleWrite(ex.Message, ConsoleWriteStyle.Error);
	//			ProgramBase.ConsoleWrite(string.Empty);
	//			throw new WDCMLSDKException();
	//		}
	//	}
	//}
}
