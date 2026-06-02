#if false
using KSP.Localization;
using System;
using UnityEngine;

namespace EditorExtensionsRedux
{
	public class StrutWindozw : GUIWindow
	{
		internal override void Awake ()
		{
			base.Awake ();
			_windowTitle = Localizer.Format("#LOC_EEX_Strutomatic");
		}

		bool _toggle = false;
		internal override void WindowContent (int windowID)
		{
			_toggle = GUILayout.Toggle (_toggle, _toggle ? Localizer.Format("#LOC_EEX_On") : Localizer.Format("#LOC_EEX_Off"), "Button");

			if (GUILayout.Button (Localizer.Format("#LOC_EEX_Close"))) {
				CloseWindow ();
			}

			GUI.DragWindow ();
		}
	}
}

#endif
