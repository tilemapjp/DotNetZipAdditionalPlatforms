namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The options for generating a self-extracting archive.
    /// </summary>
    public class SelfExtractorSaveOptions
    {
        /// <summary>
        /// Additional options for the csc.exe compiler, when producing the SFX
        /// EXE.
        /// </summary>
        /// <exclude />
        public string AdditionalCompilerSwitches { get; set; }

        /// <summary>
        /// The copyright notice, if any, to embed into the generated EXE.
        /// </summary>
        /// 
        /// <remarks>
        /// It will show up, for example, while viewing properties of the file in
        /// Windows Explorer.  You can use any arbitrary string, but typically you
        /// want something like "Copyright © Dino Chiesa 2011".
        /// </remarks>
        public string Copyright { get; set; }

        /// <summary>
        /// The default extract directory the user will see when
        /// running the self-extracting archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Passing null (or Nothing in VB) here will cause the Self Extractor to use
        /// the the user's personal directory (<see cref="F:System.Environment.SpecialFolder.Personal" />) for the default extract
        /// location.
        /// </para>
        /// 
        /// <para>
        /// This is only a default location.  The actual extract location will be
        /// settable on the command line when the SFX is executed.
        /// </para>
        /// 
        /// <para>
        /// You can specify environment variables within this string,
        /// with <c>%NAME%</c>. The value of these variables will be
        /// expanded at the time the SFX is run. Example:
        /// <c>%USERPROFILE%\Documents\unpack</c> may expand at runtime to
        /// <c>c:\users\melvin\Documents\unpack</c>.
        /// </para>
        /// </remarks>
        public string DefaultExtractDirectory { get; set; }

        /// <summary>
        /// The description to embed into the generated EXE.
        /// </summary>
        /// 
        /// <remarks>
        /// Use any arbitrary string.  This text will be displayed during a
        /// mouseover in Windows Explorer.  If you specify nothing, then the string
        /// "DotNetZip SFX Archive" is embedded into the EXE as the description.
        /// </remarks>
        public string Description { get; set; }

        /// <summary>
        /// Specify what the self-extractor will do when extracting an entry
        /// would overwrite an existing file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default behavvior is to Throw.
        /// </para>
        /// </remarks>
        public ExtractExistingFileAction ExtractExistingFile { get; set; }

        /// <summary>
        /// The file version number to embed into the generated EXE. It will show up, for
        /// example, during a mouseover in Windows Explorer.
        /// </summary>
        public Version FileVersion { get; set; }

        /// <summary>
        /// The type of SFX to create.
        /// </summary>
        public SelfExtractorFlavor Flavor { get; set; }

        /// <summary>
        /// The name of an .ico file in the filesystem to use for the application icon
        /// for the generated SFX.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Normally, DotNetZip will embed an "zipped folder" icon into the generated
        /// SFX.  If you prefer to use a different icon, you can specify it here. It
        /// should be a .ico file.  This file is passed as the <c>/win32icon</c>
        /// option to the csc.exe compiler when constructing the SFX file.
        /// </para>
        /// </remarks>
        public string IconFile { get; set; }

        /// <summary>
        /// The command to run after extraction.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is optional. Leave it empty (<c>null</c> in C# or <c>Nothing</c> in
        /// VB) to run no command after extraction.
        /// </para>
        /// 
        /// <para>
        /// If it is non-empty, the SFX will execute the command specified in this
        /// string on the user's machine, and using the extract directory as the
        /// working directory for the process, after unpacking the archive. The
        /// program to execute can include a path, if you like. If you want to execute
        /// a program that accepts arguments, specify the program name, followed by a
        /// space, and then the arguments for the program, each separated by a space,
        /// just as you would on a normal command line. Example: <c>program.exe arg1
        /// arg2</c>.  The string prior to the first space will be taken as the
        /// program name, and the string following the first space specifies the
        /// arguments to the program.
        /// </para>
        /// 
        /// <para>
        /// If you want to execute a program that has a space in the name or path of
        /// the file, surround the program name in double-quotes. The first character
        /// of the command line should be a double-quote character, and there must be
        /// a matching double-quote following the end of the program file name. Any
        /// optional arguments to the program follow that, separated by
        /// spaces. Example: <c>"c:\project files\program name.exe" arg1 arg2</c>.
        /// </para>
        /// 
        /// <para>
        /// If the flavor of the SFX is <c>SelfExtractorFlavor.ConsoleApplication</c>,
        /// then the SFX starts a new process, using this string as the post-extract
        /// command line.  The SFX waits for the process to exit.  The exit code of
        /// the post-extract command line is returned as the exit code of the
        /// command-line self-extractor exe. A non-zero exit code is typically used to
        /// indicated a failure by the program. In the case of an SFX, a non-zero exit
        /// code may indicate a failure during extraction, OR, it may indicate a
        /// failure of the run-after-extract program if specified, OR, it may indicate
        /// the run-after-extract program could not be fuond. There is no way to
        /// distinguish these conditions from the calling shell, aside from parsing
        /// the output of the SFX. If you have Quiet set to <c>true</c>, you may not
        /// see error messages, if a problem occurs.
        /// </para>
        /// 
        /// <para>
        /// If the flavor of the SFX is
        /// <c>SelfExtractorFlavor.WinFormsApplication</c>, then the SFX starts a new
        /// process, using this string as the post-extract command line, and using the
        /// extract directory as the working directory for the process. The SFX does
        /// not wait for the command to complete, and does not check the exit code of
        /// the program. If the run-after-extract program cannot be fuond, a message
        /// box is displayed indicating that fact.
        /// </para>
        /// 
        /// <para>
        /// You can specify environment variables within this string, with a format like
        /// <c>%NAME%</c>. The value of these variables will be expanded at the time
        /// the SFX is run. Example: <c>%WINDIR%\system32\xcopy.exe</c> may expand at
        /// runtime to <c>c:\Windows\System32\xcopy.exe</c>.
        /// </para>
        /// 
        /// <para>
        /// By combining this with the <c>RemoveUnpackedFilesAfterExecute</c>
        /// flag, you can create an SFX that extracts itself, runs a file that
        /// was extracted, then deletes all the files that were extracted. If
        /// you want it to run "invisibly" then set <c>Flavor</c> to
        /// <c>SelfExtractorFlavor.ConsoleApplication</c>, and set <c>Quiet</c>
        /// to true.  The user running such an EXE will see a console window
        /// appear, then disappear quickly.  You may also want to specify the
        /// default extract location, with <c>DefaultExtractDirectory</c>.
        /// </para>
        /// 
        /// <para>
        /// If you set <c>Flavor</c> to
        /// <c>SelfExtractorFlavor.WinFormsApplication</c>, and set <c>Quiet</c> to
        /// true, then a GUI with progressbars is displayed, but it is
        /// "non-interactive" - it accepts no input from the user.  Instead the SFX
        /// just automatically unpacks and exits.
        /// </para>
        /// 
        /// </remarks>
        public string PostExtractCommandLine { get; set; }

        /// <summary>
        /// The product name to embed into the generated EXE.
        /// </summary>
        /// 
        /// <remarks>
        /// Use any arbitrary string. This text will be displayed
        /// while viewing properties of the EXE file in
        /// Windows Explorer.
        /// </remarks>
        public string ProductName { get; set; }

        /// <summary>
        /// The product version to embed into the generated EXE. It will show up, for
        /// example, during a mouseover in Windows Explorer.
        /// </summary>
        /// 
        /// <remarks>
        /// You can use any arbitrary string, but a human-readable version number is
        /// recommended. For example "v1.2 alpha" or "v4.2 RC2".  If you specify nothing,
        /// then there is no product version embedded into the EXE.
        /// </remarks>
        public string ProductVersion { get; set; }

        /// <summary>
        /// Whether the ConsoleApplication SFX will be quiet during extraction.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This option affects the way the generated SFX runs. By default it is
        /// false.  When you set it to true,...
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>Flavor</term>
        /// <description>Behavior</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term><c>ConsoleApplication</c></term>
        /// <description><para>no messages will be emitted during successful
        /// operation.</para> <para> Double-clicking the SFX in Windows
        /// Explorer or as an attachment in an email will cause a console
        /// window to appear briefly, before it disappears. If you run the
        /// ConsoleApplication SFX from the cmd.exe prompt, it runs as a
        /// normal console app; by default, because it is quiet, it displays
        /// no messages to the console.  If you pass the -v+ command line
        /// argument to the Console SFX when you run it, you will get verbose
        /// messages to the console. </para>
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term><c>WinFormsApplication</c></term>
        /// <description>the SFX extracts automatically when the application
        /// is launched, with no additional user input.
        /// </description>
        /// </item>
        /// 
        /// </list>
        /// 
        /// <para>
        /// When you set it to false,...
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>Flavor</term>
        /// <description>Behavior</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term><c>ConsoleApplication</c></term>
        /// <description><para>the extractor will emit a
        /// message to the console for each entry extracted.</para>
        /// <para>
        /// When double-clicking to launch the SFX, the console window will
        /// remain, and the SFX will emit a message for each file as it
        /// extracts. The messages fly by quickly, they won't be easily
        /// readable, unless the extracted files are fairly large.
        /// </para>
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term><c>WinFormsApplication</c></term>
        /// <description>the SFX presents a forms UI and allows the user to select
        /// options before extracting.
        /// </description>
        /// </item>
        /// 
        /// </list>
        /// 
        /// </remarks>
        public bool Quiet { get; set; }

        /// <summary>
        /// Whether to remove the files that have been unpacked, after executing the
        /// PostExtractCommandLine.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// If true, and if there is a <see cref="P:SelfExtractorSaveOptions.PostExtractCommandLine">
        /// PostExtractCommandLine</see>, and if the command runs successfully,
        /// then the files that the SFX unpacked will be removed, afterwards.  If
        /// the command does not complete successfully (non-zero return code),
        /// that is interpreted as a failure, and the extracted files will not be
        /// removed.
        /// </para>
        /// 
        /// <para>
        /// Setting this flag, and setting <c>Flavor</c> to
        /// <c>SelfExtractorFlavor.ConsoleApplication</c>, and setting <c>Quiet</c> to
        /// true, results in an SFX that extracts itself, runs a file that was
        /// extracted, then deletes all the files that were extracted, with no
        /// intervention by the user.  You may also want to specify the default
        /// extract location, with <c>DefaultExtractDirectory</c>.
        /// </para>
        /// 
        /// </remarks>
        public bool RemoveUnpackedFilesAfterExecute { get; set; }

        /// <summary>
        /// The title to display in the Window of a GUI SFX, while it extracts.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// By default the title show in the GUI window of a self-extractor
        /// is "DotNetZip Self-extractor (http://DotNetZip.codeplex.com/)".
        /// You can change that by setting this property before saving the SFX.
        /// </para>
        /// 
        /// <para>
        /// This property has an effect only when producing a Self-extractor
        /// of flavor <c>SelfExtractorFlavor.WinFormsApplication</c>.
        /// </para>
        /// </remarks>
        public string SfxExeWindowTitle { get; set; }
    }
}

