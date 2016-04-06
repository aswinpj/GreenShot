/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using Greenshot.IniFile;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GreenshotPicasaPlugin
{
	/// <summary>
	/// This is the Picasa base code
	/// </summary>
	public class PicasaPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PicasaPlugin));
		private static PicasaConfiguration config;
		public static PluginAttribute Attributes;
		private IGreenshotHost _host;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInRoot;

		public void Dispose() {
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_itemPlugInRoot != null) {
					_itemPlugInRoot.Dispose();
					_itemPlugInRoot = null;
				}
			}
		}

		public PicasaPlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new PicasaDestination(this);
		}

		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			this._host = (IGreenshotHost)pluginHost;
			Attributes = myAttributes;

			// Get configuration
			config = IniConfig.GetIniSection<PicasaConfiguration>();
			_resources = new ComponentResourceManager(typeof(PicasaPlugin));

			_itemPlugInRoot = new ToolStripMenuItem();
			_itemPlugInRoot.Text = Language.GetString("picasa", LangKey.Configure);
			_itemPlugInRoot.Tag = _host;
			_itemPlugInRoot.Image = (Image)_resources.GetObject("Picasa");
			_itemPlugInRoot.Click += new System.EventHandler(ConfigMenuClick);
			PluginUtils.AddToContextMenu(_host, _itemPlugInRoot);
			Language.LanguageChanged += new LanguageChangedHandler(OnLanguageChanged);
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInRoot != null) {
				_itemPlugInRoot.Text = Language.GetString("picasa", LangKey.Configure);
			}
		}

		public virtual void Shutdown() {
			LOG.Debug("Picasa Plugin shutdown.");
			Language.LanguageChanged -= new LanguageChangedHandler(OnLanguageChanged);
			//host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			config.ShowConfigDialog();
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e) {
			LOG.Debug("Application closing, de-registering Picasa Plugin!");
			Shutdown();
		}

		public void ConfigMenuClick(object sender, EventArgs eventArgs) {
			Configure();
		}

		public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality);
			try {
				string url = null;
				new PleaseWaitForm().ShowAndWait(PicasaPlugin.Attributes.Name, Language.GetString("picasa", LangKey.communication_wait), 
					delegate() {
						string filename = Path.GetFileName(FilenameHelper.GetFilename(config.UploadFormat, captureDetails));
						string contentType = "image/" + config.UploadFormat.ToString();
						url = PicasaUtils.UploadToPicasa(surfaceToUpload, outputSettings, captureDetails.Title, filename);
					}
				);
				uploadUrl = url;

				if (uploadUrl != null && config.AfterUploadLinkToClipBoard) {
					ClipboardHelper.SetClipboardData(uploadUrl);
				}
				return true;
			} catch (Exception e) {
				LOG.Error("Error uploading.", e);
				MessageBox.Show(Language.GetString("picasa", LangKey.upload_failure) + " " + e.Message);
			}
			uploadUrl = null;
			return false;
		}
	}
}
