using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WDCMLSDKBase;
using WDCMLSDKDerived;

namespace WDCMLSDK
{
	/// <summary>
	/// An object model representing the Win32 API ref docs.
	/// </summary>
	internal class ApiRefModelWin32
	{
		/// <summary>
		/// Gets an object model representing the Win32 API ref docs.
		/// </summary>
		public static ApiRefModelWin32 GetApiRefModelWin32
		{
			get
			{
				ProgramBase.ConsoleWrite("Creating Win32 docset", ConsoleWriteStyle.Success);

				List<string> desktopDotTxt = null;
				ProgramBase.LoadTextFileIntoStringList("desktop.txt", ref desktopDotTxt, "MISSING desktop.txt. This file could not be found in your enlistment folder path. Your configuration.txt contains something like: my_enlistment_folder D:\\Source_Depot\\devdocmain. This should be the folder that contains the dev_*, m_*, w_* folders, BuildX, desktop.txt, etc.");

				string projectListIntro = "These are the shipping projects that document Win32 functions (they're in desktop.txt). The app only processes topics in these projects that are represented by an unfiltered TOC entry (that is, no MSDN build condition).";

				ProgramBase.ConsoleWrite(projectListIntro, ConsoleWriteStyle.Highlight);

				List<DirectoryInfo> projectDirectoryInfos = new List<DirectoryInfo>();

				foreach (string eachProjectName in desktopDotTxt)
				{
					projectDirectoryInfos.AddRange(ProgramBase.EnlistmentDirectoryInfo.GetDirectories(eachProjectName, SearchOption.TopDirectoryOnly).ToList());
				}

				ApiRefModelWin32 apiRefModelWin32 = new ApiRefModelWin32();

				foreach (DirectoryInfo eachProjectDirectoryInfo in projectDirectoryInfos)
				{
					ProgramBase.ConsoleWrite(eachProjectDirectoryInfo.Name, ConsoleWriteStyle.Default, false);
					if (eachProjectDirectoryInfo != projectDirectoryInfos[projectDirectoryInfos.Count - 1])
					{
						ProgramBase.ConsoleWrite(", ", ConsoleWriteStyle.Default, false);
					}
					else
					{
						ProgramBase.ConsoleWrite(".\n\n");
					}
					// For reference projects, the topic files should be in a folder with the same name as the project folder. But use the standard algorithm just to be sure.
					apiRefModelWin32.ProcessProject(eachProjectDirectoryInfo.Name, EditorBase.GetEditorsForTopicsInProject(eachProjectDirectoryInfo), ref apiRefModelWin32);
				}

				return apiRefModelWin32;
			}
		}

		// Note: the key here is the ToLower version but the FunctionWin32 contains the original case from the topic's title.
		public Dictionary<string, FunctionWin32InDocs> FunctionWin32InDocses = new Dictionary<string, FunctionWin32InDocs>();

		public void ProcessProject(string projectName, List<Editor> topicEditors, ref ApiRefModelWin32 apiRefModelWin32)
		{
			foreach (Editor eachTopicEditor in topicEditors)
			{
				string eachTopicMetadataAtId = eachTopicEditor.GetMetadataAtId();
				string eachTopicTitle = eachTopicEditor.GetMetadataAtTitle();
				FileInfo eachTopicFileInfo = eachTopicEditor.FileInfo;
				if (eachTopicEditor.GetMetadataAtTypeAsString() == "function")
				{
					bool functionAlreadyExists = false;
					FunctionWin32InDocs functionWin32 = null;

					this.EnsureFunction(projectName, eachTopicMetadataAtId, eachTopicTitle, eachTopicFileInfo, ref functionAlreadyExists, ref functionWin32);
					if (functionAlreadyExists)
					{
						ProgramBase.DupedWin32ApiNamesLog.AddEntry(string.Format("{0} and {1}", eachTopicFileInfo.FullName, functionWin32.FileInfo.FullName));
					}
				}
			}
		}

		public void EnsureFunction(string projectName, string id, string name, FileInfo fileInfo, ref bool functionAlreadyExists, ref FunctionWin32InDocs functionWin32)
		{
			if (this.FunctionWin32InDocses.ContainsKey(name.ToLower()))
			{
				functionAlreadyExists = true;
				functionWin32 = this.FunctionWin32InDocses[name.ToLower()];
			}
			else
			{
				functionAlreadyExists = false;
				this.FunctionWin32InDocses[name.ToLower()] = new FunctionWin32InDocs(projectName, id, name, fileInfo);
			}
		}

		public bool HasFunctionWin32s
		{
			get
			{
				return (FunctionWin32InDocses.Count != 0);
			}
		}

		public bool GetFunctionWin32ByName(string name, ref FunctionWin32InDocs foundFunctionWin32)
		{
			if (this.FunctionWin32InDocses.ContainsKey(name.ToLower()))
			{
				foundFunctionWin32 = this.FunctionWin32InDocses[name.ToLower()];
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// A class representing a Win32 function.
	/// </summary>
	internal class FunctionWin32
	{
		public string Name { get; set; }
		public string ModuleName { get; set; }

		public FunctionWin32(string name, string moduleName)
		{
			this.Name = name;
			this.ModuleName = moduleName;
		}
	}

	/// <summary>
	/// Utility class used for sorting FunctionWin32s.
	/// </summary>
	internal class FunctionWin32Comparer : Comparer<FunctionWin32>
	{
		public override int Compare(FunctionWin32 lhs, FunctionWin32 rhs)
		{
			return lhs.Name.CompareTo(rhs.Name);
		}
	}

	/// <summary>
	/// Utility class used for sorting FunctionWin32GroupedByInitialChars.
	/// </summary>
	internal class FunctionWin32GroupedByInitialCharComparer : Comparer<FunctionWin32GroupedByInitialChar>
	{
		public override int Compare(FunctionWin32GroupedByInitialChar lhs, FunctionWin32GroupedByInitialChar rhs)
		{
			return lhs.Name.CompareTo(rhs.Name);
		}
	}

	/// <summary>
	/// Utility class used for sorting InitialCharGroups.
	/// </summary>
	internal class InitialCharGroupComparer : Comparer<InitialCharGroup>
	{
		public override int Compare(InitialCharGroup lhs, InitialCharGroup rhs)
		{
			return lhs.Name.CompareTo(rhs.Name);
		}
	}

	/// <summary>
	/// A class representing a Win32 function in the docs.
	/// </summary>
	internal class FunctionWin32InDocs : FunctionWin32
	{
		public FunctionWin32InDocs(string projectName, string id, string name, FileInfo fileInfo)
			: base(name, null)
		{
			this.ProjectName = projectName;
			this.Id = id;
			this.Name = name;
			this.FileInfo = fileInfo;
		}

		public string ProjectName = string.Empty;
		public string Id = string.Empty;
		public FileInfo FileInfo;
	}

	/// <summary>
	/// A class representing an api grouped by some key.
	/// </summary>
	internal class FunctionWin32Grouped : FunctionWin32
	{
		public string SdkVersionIntroducedIn { get; set; }
		public string SdkVersionRemovedIn { get; set; }
		public string FunctionWin32Id { get; set; }
		public Module ModuleMovedTo { get; set; }

		public FunctionWin32Grouped(string moduleName, string name, string sdkVersionIntroducedIn, string functionWin32Id)
			: base(name, moduleName)
		{
			this.SdkVersionIntroducedIn = sdkVersionIntroducedIn;
			this.FunctionWin32Id = functionWin32Id;
		}
	}

	internal class FunctionWin32GroupedByModule : FunctionWin32Grouped
	{
		public string Requirements
		{
			get
			{
				string requirements = "Introduced in Windows " + this.SdkVersionIntroducedIn;
				if (this.SdkVersionRemovedIn != null)
				{
					if (this.ModuleMovedTo != null)
					{
						requirements += ". Moved to " + this.ModuleMovedTo.Name + " in Windows " + this.SdkVersionRemovedIn;
					}
					else
					{
						requirements += ". Removed in Windows " + this.SdkVersionRemovedIn;
					}
				}
				return requirements;
			}
		}

		public FunctionWin32GroupedByModule(string moduleName, string name, string sdkVersionIntroducedIn, string functionWin32Id)
			: base(moduleName, name, sdkVersionIntroducedIn, functionWin32Id)
		{
		}
	}

	internal class FunctionWin32GroupedByInitialChar : FunctionWin32Grouped
	{
		public string Module
		{
			get
			{
				string requirements = "Introduced into " + this.ModuleName + " in Windows " + this.SdkVersionIntroducedIn;
				if (this.SdkVersionRemovedIn != null)
				{
					if (this.ModuleMovedTo != null)
					{
						requirements += ". Moved to " + this.ModuleMovedTo.Name + " in Windows " + this.SdkVersionRemovedIn;
					}
					else
					{
						requirements += ". Removed in Windows " + this.SdkVersionRemovedIn;
					}
				}
				return requirements;
			}
		}

		public FunctionWin32GroupedByInitialChar(string moduleName, string name, string sdkVersionIntroducedIn, string functionWin32Id, string sdkVersionRemovedIn, Module moduleMovedTo)
			: base(moduleName, name, sdkVersionIntroducedIn, functionWin32Id)
		{
			this.SdkVersionRemovedIn = sdkVersionRemovedIn;
			this.ModuleMovedTo = moduleMovedTo;
		}
	}

	/// <summary>
	/// A class representing an api set or dll.
	/// </summary>
	internal class Module
	{
		public string Name { get; set; }
		public bool IsApiSet { get; set; }
		private List<FunctionWin32GroupedByModule> apis = new List<FunctionWin32GroupedByModule>();

		public System.Collections.ObjectModel.ReadOnlyCollection<FunctionWin32GroupedByModule> Apis
		{
			get { return new System.Collections.ObjectModel.ReadOnlyCollection<FunctionWin32GroupedByModule>(this.apis); }
		}

		public Module(string name, bool isApiSet)
		{
			this.Name = name;
			this.IsApiSet = isApiSet;
			this.apis = new List<FunctionWin32GroupedByModule>();
		}

		public void AddApi(string name, string sdkVersionIntroducedIn, string functionWin32Id)
		{
			this.apis.Add(new FunctionWin32GroupedByModule(this.Name, name, sdkVersionIntroducedIn, functionWin32Id));
		}

		public FunctionWin32GroupedByModule FindApi(string name)
		{
			return this.apis.Find(found => found.Name == name);
		}
	}

	/// <summary>
	/// A class representing apis grouped by initial char.
	/// </summary>
	internal class InitialCharGroup
	{
		public string Name { get; set; }
		private List<FunctionWin32GroupedByInitialChar> apis = new List<FunctionWin32GroupedByInitialChar>();

		public System.Collections.ObjectModel.ReadOnlyCollection<FunctionWin32GroupedByInitialChar> Apis
		{
			get { return new System.Collections.ObjectModel.ReadOnlyCollection<FunctionWin32GroupedByInitialChar>(this.apis); }
		}

		public InitialCharGroup(string name)
		{
			this.Name = name;
			this.apis = new List<FunctionWin32GroupedByInitialChar>();
		}

		public void AddApi(string moduleName, string name, string sdkVersionIntroducedIn, string functionWin32Id, string sdkVersionRemovedIn, Module moduleMovedTo)
		{
			this.apis.Add(new FunctionWin32GroupedByInitialChar(moduleName, name, sdkVersionIntroducedIn, functionWin32Id, sdkVersionRemovedIn, moduleMovedTo));
		}

		public FunctionWin32GroupedByInitialChar FindApi(string name)
		{
			return this.apis.Find(found => found.Name == name);
		}

		public void Sort()
		{
			this.apis.Sort(new FunctionWin32GroupedByInitialCharComparer());
		}

		public static List<InitialCharGroup> ListOfInitialCharGroupFromListOfModule(List<Module> modules)
		{
			List<InitialCharGroup> initialCharGroups = new List<InitialCharGroup>();

			foreach (Module module in modules)
			{
				foreach (FunctionWin32GroupedByModule functionWin32GroupedByModule in module.Apis)
				{
					string initialCharGroupKey = functionWin32GroupedByModule.Name.Substring(0, 1);
					if (char.IsLetter(initialCharGroupKey[0]))
					{
						initialCharGroupKey = initialCharGroupKey.ToUpper();
					}
					else
					{
						initialCharGroupKey = "_";
					}
					InitialCharGroup initialCharGroup = initialCharGroups.Find(found => found.Name == initialCharGroupKey);
					if (initialCharGroup == null)
					{
						initialCharGroup = new InitialCharGroup(initialCharGroupKey);
						initialCharGroups.Add(initialCharGroup);
					}
					initialCharGroup.AddApi(functionWin32GroupedByModule.ModuleName, functionWin32GroupedByModule.Name, functionWin32GroupedByModule.SdkVersionIntroducedIn, functionWin32GroupedByModule.FunctionWin32Id, functionWin32GroupedByModule.SdkVersionRemovedIn, functionWin32GroupedByModule.ModuleMovedTo);
				}
			}

			foreach (InitialCharGroup initialCharGroup in initialCharGroups)
			{
				initialCharGroup.Sort();
			}
			initialCharGroups.Sort(new InitialCharGroupComparer());

			return initialCharGroups;
		}
	}

	/// <summary>
	/// A class representing the API Sets in an umbrella lib.
	/// </summary>
	internal class UmbrellaLib
	{
		public string Name { get; set; }
		public List<Module> Modules = new List<Module>();
		public List<InitialCharGroup> InitialCharGroups = null;

		public Dictionary<string, List<string>> setToApiMappings = new Dictionary<string, List<string>>();

		public UmbrellaLib(string name)
		{
			this.Name = name;
		}

		public Module GetModuleForApiName(string apiName)
		{
			foreach (Module module in this.Modules)
			{
				FunctionWin32Grouped apiToAdd = module.FindApi(apiName);
				if (apiToAdd != null)
				{
					return module;
				}
			}
			return null;
		}

		public void AddApi(Microsoft.CoreSystem.WindowsCompositionDatabase.Database.Api api, string sdkVersion, string functionWin32Id)
		{
			bool isApiSet = false;
			foreach (Microsoft.CoreSystem.WindowsCompositionDatabase.Database.Apiset apiset in api.Apisets) // api.Apisets can contain many entries: 6 is not unusual.
			{
				isApiSet = true; break;
			}

			Module module = this.Modules.Find(found => found.Name == api.Binary.Name);
			if (module == null)
			{
				module = new Module(api.Binary.Name, isApiSet);
				this.Modules.Add(module);
			}

			FunctionWin32Grouped apiToAdd = module.FindApi(api.Name);

			if (apiToAdd == null)
			{
				module.AddApi(api.Name, sdkVersion, functionWin32Id);
			}
		}

		public void SortAlphabetically()
		{
			this.InitialCharGroups = InitialCharGroup.ListOfInitialCharGroupFromListOfModule(this.Modules);
		}

	}
}