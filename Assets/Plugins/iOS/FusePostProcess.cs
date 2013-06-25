using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

public static class FusePostProcess
{
    // Frameworks Ids  -  These ids have been generated by creating a project using Xcode then
    // extracting the values from the generated project.pbxproj.  The format of this
    // file is not documented by Apple so the correct algorithm for generating these
    // ids is unknown
    const string CORETELEPHONY_ID = "3F3EE17B1757FB570038DED5";
    const string ADSUPPORT_ID = "3F3EE1791757FB4D0038DED6";
	const string STOREKIT_ID = "3F3EE17D1757FB610038DED7";
	const string CORETELEPHONY_FW = "3F3EE17B1757FB570038DED8";
    const string ADSUPPORT_FW = "3F3EE1791757FB4D0038DED9";
	const string STOREKIT_FW = "3F3EE17D1757FB610038DED0";
    
    // List of all the frameworks to be added to the project
    public struct framework
    {
        public string sName;
        public string sId;
        public string sFileId;
        
        public framework(string name, string myId, string fileid)
        {
            sName = name;
            sId = myId;
            sFileId = fileid;
        }
    }

#if UNITY_EDITOR
	/// Processbuild Function
    [PostProcessBuild] // <- this is where the magic happens
    public static void OnPostProcessBuild(BuildTarget target, string path)	
    {
        // 1: Check this is an iOS build before running
#if UNITY_IPHONE		
        {
            // 2: We init our tab and process our project
            framework[] myFrameworks = { new framework("CoreTelephony.framework", CORETELEPHONY_FW, CORETELEPHONY_ID),
										 new framework("AdSupport.framework", ADSUPPORT_FW, ADSUPPORT_ID),
										 new framework("StoreKit.framework", STOREKIT_FW, STOREKIT_ID)};
			            
            string xcodeprojPath = EditorUserBuildSettings.GetBuildLocation(EditorUserBuildSettings.activeBuildTarget);			
		
			xcodeprojPath = xcodeprojPath + "/Unity-iPhone.xcodeproj";
			Debug.Log("We found xcodeprojPath to be : "+xcodeprojPath);
			
            updateXcodeProject(xcodeprojPath, myFrameworks);			
        }
#else
        // 3: We do nothing if not iPhone
//        Debug.Log("OnPostProcessBuild - Warning: This is not an iOS build");
		return;
#endif // UNITY IPHONE
        Debug.Log("OnPostProcessBuild - STOP");
    }
#endif // UNITY_EDITOR
    
    
    
    // MAIN FUNCTION
    // xcodeproj_filename - filename of the Xcode project to change
    // frameworks - list of Apple standard frameworks to add to the project
	static bool bFoundCore = false;
	static bool bFoundAd = false;
	static bool bFoundStore = false;
    public static void updateXcodeProject(string xcodeprojPath, framework[] listeFrameworks)
    {
        // STEP 1 : We open up the file generated by Unity and read into memory as
        // a list of lines for processing
        string project = xcodeprojPath + "/project.pbxproj";
        string[] lines = System.IO.File.ReadAllLines(project);
		
        // STEP 2 : We process only the missing frameworks
        int i = 0;        
        bool bEnd = false;
        while ( !bEnd)
        {
            if (lines[i].Length > 5 && (string.Compare(lines[i].Substring(3, 3), "End") == 0) )
                bEnd = true;
            
			if( lines[i].Contains("CoreTelephony.framework") )
			{
				bFoundCore = true;
			}
			else if( lines[i].Contains("AdSupport.framework") )
			{
				bFoundAd = true;
			}
			else if( lines[i].Contains("StoreKit.framework") )
			{
				bFoundStore = true;
			}
            
            ++i;
        }		
        
		
        // STEP 3 : We'll open/replace project.pbxproj for writing and iterate over the old
        // file in memory, copying the original file and inserting every extra we need
        FileStream filestr = new FileStream(project, FileMode.Create); //Create new file and open it for read and write, if the file exists overwrite it.
        filestr.Close();
        StreamWriter fCurrentXcodeProjFile = new StreamWriter(project); // will be used for writing
        
        // As we iterate through the list we'll record which section of the
        // project.pbxproj we are currently in
        string section = "";
        
        // We use this boolean to decide whether we have already added the list of
        // build files to the link line.  This is needed because there could be multiple
        // build targets and they are not named in the project.pbxproj
        bool bFrameworks_build_added = false;
        int iNbBuildConfigSet = 0; // can't be > 2
        
        i = 0;
        foreach (string line in lines)
        {
            fCurrentXcodeProjFile.WriteLine(line);            
						
            //////////////////////////////////
            //  STEP 2 : Include Framewoks  //
            //////////////////////////////////                    
            // Each section starts with a comment such as : /* Begin PBXBuildFile section */'
            if ( lines[i].Length > 7 && string.Compare(lines[i].Substring(3, 5), "Begin") == 0  )
            {
                section = line.Split(' ')[2];
                //Debug.Log("NEW_SECTION: "+section);
                if (section == "PBXBuildFile")
                {
                    foreach (framework fr in listeFrameworks)
                    add_build_file(fCurrentXcodeProjFile, fr.sId, fr.sName, fr.sFileId);
                }
                
                if (section == "PBXFileReference")
                {
                    foreach (framework fr in listeFrameworks)
                    add_framework_file_reference(fCurrentXcodeProjFile, fr.sFileId, fr.sName);
                }
                
                if (line.Length > 5 && string.Compare(line.Substring(3, 3), "End") == 0)
                    section = "";
            }
            // The PBXResourcesBuildPhase section is what appears in XCode as 'Link
            // Binary With Libraries'.  As with the frameworks we make the assumption the
            // first target is always 'Unity-iPhone' as the name of the target itself is
            // not listed in project.pbxproj
            if (section == "PBXFrameworksBuildPhase" &&
                line.Trim().Length > 4 &&
                string.Compare(line.Trim().Substring(0, 5) , "files") == 0 &&
                !bFrameworks_build_added)
            {
                foreach (framework fr in listeFrameworks)
                add_frameworks_build_phase(fCurrentXcodeProjFile, fr.sId, fr.sName);
                bFrameworks_build_added = true;
            }
            
            // The PBXGroup is the section that appears in XCode as 'Copy Bundle Resources'.
            if (section == "PBXGroup" &&
                line.Trim().Length > 7 &&
                string.Compare(line.Trim().Substring(0, 8) , "children") == 0 &&
                lines[i-2].Trim().Split(' ').Length > 0 &&
                string.Compare(lines[i-2].Trim().Split(' ')[2] , "CustomTemplate" ) == 0 )
            {						
                foreach (framework fr in listeFrameworks)
                add_group(fCurrentXcodeProjFile, fr.sFileId, fr.sName);
            }
            
            //////////////////////////////
            //  STEP 3 : Build Options  //
            //////////////////////////////
            if (section == "XCBuildConfiguration" &&
                line.StartsWith("\t\t\t\tOTHER_LDFLAGS") &&
                iNbBuildConfigSet < 2)
            {
				int j = 0;
				bool bFlagSet = false;				
				while( string.Compare(lines[i+j].Trim(), "};") != 0 )
				{
					if( lines[i+j].Contains("ObjC") )
					{
						bFlagSet = true;
					}
					j++;
				}
				if( !bFlagSet )
				{
	                //fCurrentXcodeProjFile.Write("\t\t\t\t\t\"-all_load\",\n");
	                fCurrentXcodeProjFile.Write("\t\t\t\t\t\"-ObjC\",\n");
	                Debug.Log("OnPostProcessBuild - Adding \"-ObjC\" flag to build options"); // \"-all_load\" and
				}
                ++iNbBuildConfigSet;
            }
			i++;					
        }
        fCurrentXcodeProjFile.Close();        
    }
    
    
    /////////////////
    ///////////
    // ROUTINES
    ///////////
    /////////////////
    
    // check to see if the framework has already been processed
	private static bool should_process_framework(string name)
	{
		if( (bFoundCore && name.Equals("CoreTelephony.framework"))
			|| (bFoundAd && name.Equals("AdSupport.framework")) 
			|| (bFoundStore && name.Equals("StoreKit.framework")) )
		{
			// framework is already in the xcode project - do no process it
			return false;
		}
		
		// the framework doesn't exist in the xcode project
		return true;
	}
	
    // Adds a line into the PBXBuildFile section
    private static void add_build_file(StreamWriter file, string id, string name, string fileref)
    {
		// do not re-add the framework if it already exists in the xcode project
		if( !should_process_framework(name) )
		{
			//Debug.Log(name + " already exists in xcode project");
			return;
		}
		
        Debug.Log("OnPostProcessBuild - Adding build file " + name);
        string subsection = "Frameworks";
        
        // optional frameworks (currently all)
		file.Write("\t\t"+id+" /* "+name+" in "+subsection+" */ = {isa = PBXBuildFile; fileRef = "+fileref+" /* "+name+" */; settings = {ATTRIBUTES = (Weak, ); }; };");
//        else // required frameworks
//            file.Write("\t\t"+id+" /* "+name+" in "+subsection+" */ = {isa = PBXBuildFile; fileRef = "+fileref+" /* "+name+" */; };\n");
    }
    
    // Adds a line into the PBXBuildFile section
    private static void add_framework_file_reference(StreamWriter file, string id, string name)
    {
		// do not re-add the framework if it already exists in the xcode project
		if( !should_process_framework(name) )
		{
			return;
		}
		
        Debug.Log("OnPostProcessBuild - Adding framework file reference " + name);
        
        string path = "System/Library/Frameworks"; // all the frameworks come from here
        if (name == "libsqlite3.0.dylib")           // except for lidsqlite
            path = "usr/lib";
        
        file.Write("\t\t"+id+" /* "+name+" */ = {isa = PBXFileReference; lastKnownFileType = wrapper.framework; name = "+name+"; path = "+path+"/"+name+"; sourceTree = SDKROOT; };\n");
    }
    
    // Adds a line into the PBXFrameworksBuildPhase section
    private static void add_frameworks_build_phase(StreamWriter file, string id, string name)
    {
		// do not re-add the framework if it already exists in the xcode project
		if( !should_process_framework(name) )
		{
			return;
		}
        Debug.Log("OnPostProcessBuild - Adding build phase " + name);
        
        file.Write("\t\t\t\t"+id+" /* "+name+" in Frameworks */,\n");
    }
    
    // Adds a line into the PBXGroup section
    private static void add_group(StreamWriter file, string id, string name)
    {
		// do not re-add the framework if it already exists in the xcode project
		if( !should_process_framework(name) )
		{
			return;
		}
        Debug.Log("OnPostProcessBuild - Add group " + name);
        
        file.Write("\t\t\t\t"+id+" /* "+name+" */,\n");
    }	
}