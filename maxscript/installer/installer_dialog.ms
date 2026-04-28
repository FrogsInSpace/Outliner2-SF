fileIn "installer.ms";

/**
 * PRE INSTALL DIALOG (CHOOSE INSTALL / UNINSTALL)
 */
rollout outliner_pre_install ((outlinerInstaller.getString #title) + " " + \
                              (outlinerInstaller.getVersion asString:true) + \
                              " installation")
(
   label title_lbl "" offset:[0, 5];
   label build_lbl "" offset:[0, 0];
   label descr_lbl "" offset:[0, 5];
   label descr_lbl2 "" offset:[0, -2];
   button install_btn "Install" width:60 offset:[0, 10] across:3
   button uninstall_btn "Uninstall" width:60 offset:[0, 10]
   button close_btn "Close" width:60 offset:[0, 10]

   on outliner_pre_install open do
   (
      title_lbl.text = (outlinerInstaller.getString #title) + " " + (outlinerInstaller.getVersion asString:true);

      if( classof outlinerInstaller.build_info == Array AND outlinerInstaller.build_info.count >= 4) do
      (
         build_lbl.text = "Build: "	+ (outlinerInstaller.build_info[1] as string) + "." \
                                    + (outlinerInstaller.build_info[2] as string) + "." \
                                    + (outlinerInstaller.build_info[3] as string) + "." \
                                    + (outlinerInstaller.build_info[4] as string) + "-" \
                                    + (outlinerInstaller.build_info[5] as string)
      )

	   descr_lbl.text = outlinerInstaller.getString #descr_lbl1;
	   descr_lbl2.text = outlinerInstaller.getString #descr_lbl2;

	   local isInstalled = outlinerInstaller.getIsOutlinerInstalled();
      local oldVersion  = outlinerInstaller.getInstalledVersion();
	   local version     = outlinerInstaller.getVersion();
      if (not isInstalled OR oldVersion == 0) then
         install_btn.text = "Install";
      else if (oldVersion < version) then
         install_btn.text = "Update";
      else if (oldVersion == version) then
         install_btn.text = "Reinstall";
   )

   on install_btn pressed do
   (
      destroyDialog outliner_pre_install;
      outlinerInstaller.install();
   )

   on uninstall_btn pressed do
   (
      destroyDialog outliner_pre_install;
      outlinerInstaller.uninstall();
   )

   on close_btn pressed do
   (
      destroyDialog outliner_pre_install;
   )
)--End rollout.


createDialog outliner_pre_install 420 \
                                  120 \
                                  modal:true \
                                  lockWidth:true \
                                  lockHeight:true \
                                  style:#(#style_sysmenu, #style_toolwindow);