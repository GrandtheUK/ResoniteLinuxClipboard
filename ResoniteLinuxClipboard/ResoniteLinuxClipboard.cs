using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Elements.Assets;
using Renderite.Shared;
using System.Threading.Tasks;
using FrooxEngine.UIX;
using Elements.Core;

namespace ResoniteLinuxClipboard;

public partial class ResoniteLinuxClipboard : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0"; 
	public override string Name => "ResoniteLinuxClipboard";
	public override string Author => "Grand";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/resonite-modding-group/ExampleMod/";

	public override void OnEngineInit() {
		Harmony harmony = new Harmony("com.example.ExampleMod");
		harmony.PatchAll();
		Engine.Current.RunPostInit( () => {
			Engine.Current.InputInterface.RegisterClipboardInterface(new ResoniteLinuxClipboardInterface());
		});
		// Console.WriteLine("mod loaded but not active");

	}
	public partial class ResoniteLinuxClipboardInterface : IClipboardInterface {
		public bool ContainsText => false;

		public bool ContainsFiles => false;

		public bool ContainsImage => false;

		public void Dispose() {
			Console.WriteLine("disposing");
		}

		public List<string> GetFiles() {
			return [""];
		}

		public Bitmap2D GetImage() {
			Bitmap2D bitmap = new([], 0, 0, TextureFormat.BC7, false, ColorProfile.sRGBAlpha);
			return bitmap;
		}

		public string GetText() {
			return "";
		}

		public void SetBitmap(Bitmap2D bitmap) {
			int count = bitmap.ElementTotalBytes;
			byte[] bytes = bitmap.RawData.ToArray();
			copy(bytes,(uint)count);
			Console.WriteLine("Attempted copy image");
		}

		public void SetText(string text) {
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			copy(bytes, (uint)bytes.Length);
			Console.WriteLine("Attempted copy text");
		}

		async Task<string> IClipboardInterface.GetText() {
			string result = GetText();
			await Task.Delay(10);
			return result;
		}

		Task<List<string>> IClipboardInterface.GetFiles() {
			return Task.FromResult(GetFiles());
		}

		Task<Bitmap2D> IClipboardInterface.GetImage() {
			return Task.FromResult(GetImage());
		}

		Task<bool> IClipboardInterface.SetText(string text) {
			SetText(text);
			return Task.FromResult(true);
		}

		Task<bool> IClipboardInterface.SetBitmap(Bitmap2D bitmap) {
			SetBitmap(bitmap);
			return Task.FromResult(true);
		}


		[LibraryImport("libresoniteclipboard_rs.so")]
		private static partial void copy(byte[] data, uint data_length);
	}
}
