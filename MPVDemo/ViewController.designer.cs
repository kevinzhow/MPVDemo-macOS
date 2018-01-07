// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace MPVDemo
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSTextField TextLabel { get; set; }

		[Outlet]
		AppKit.NSView VideoView { get; set; }

		[Action ("ChooseFile:")]
		partial void ChooseFile (Foundation.NSObject sender);

		[Action ("MakeScreenShot:")]
		partial void MakeScreenShot (Foundation.NSObject sender);

		[Action ("Pause:")]
		partial void Pause (Foundation.NSObject sender);

		[Action ("Play:")]
		partial void Play (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (TextLabel != null) {
				TextLabel.Dispose ();
				TextLabel = null;
			}

			if (VideoView != null) {
				VideoView.Dispose ();
				VideoView = null;
			}
		}
	}
}
