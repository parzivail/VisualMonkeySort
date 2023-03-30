using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using TinyDialogsNet;

namespace VisualMonkeySort;

public record ComparisonKey(string X, string Y);
public record ComparisonImages(ComparisonKey Key, int ImageX, int ImageY);

public class UndefinedComparisonException : Exception
{
	public ComparisonKey Key { get; }

	public UndefinedComparisonException(ComparisonKey key)
	{
		Key = key;
	}
}

public class MonkeyComparer : IComparer<string>
{
	public readonly Dictionary<ComparisonKey, int> ComparisonMatrix = new();

	public int Compare(string? x, string? y)
	{
		if (x == null || y == null)
			throw new Exception();

		if (x == y)
			return 0;

		var key = new ComparisonKey(x, y);
		if (ComparisonMatrix.TryGetValue(key, out var value)) return value;
		if (ComparisonMatrix.TryGetValue(new ComparisonKey(y, x), out var negValue)) return -negValue;
		throw new UndefinedComparisonException(key);
	}

	public void InsertComparison(string greater, string lesser)
	{
		ComparisonMatrix[new ComparisonKey(greater, lesser)] = 1;
	}
}

public class Window : ImGuiWindow
{
	private MonkeyComparer _comparer = new();
	private List<string>? _filenames;

	private ComparisonImages? _currentComparison;

	private Dictionary<string, int> _imageHandles = new();

	public override void Process()
	{
		var flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.MenuBar;
		flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground;
		flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoDecoration;

		var v = ImGui.GetMainViewport();

		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(v.Size);

		if (ImGui.Begin("Visual MonkeySort", flags))
		{
			if (ImGui.BeginMenuBar())
			{
				if (ImGui.BeginMenu("File"))
				{
					if (ImGui.MenuItem("Open Folder"))
					{
						var result = Dialogs.OpenFileDialog(allowMultipleSelects: true);
						if (result != null)
						{
							_filenames = result.ToList();
							var listedFiles = InitComparisonMatrix();
							if (_filenames.Count == 1 && Path.GetExtension(_filenames[0]) == ".mky")
								_filenames = new List<string>(listedFiles);
						}
						TrySort();
					}

					ImGui.EndMenu();
				}

				ImGui.EndMenuBar();
			}

			if (_currentComparison != null)
			{
				if (ImGui.ImageButton("imgX", _currentComparison.ImageX, new Vector2(512, 512)))
				{
					_comparer.InsertComparison(_currentComparison.Key.X, _currentComparison.Key.Y);
					TrySort();
				}
				else
				{
					ImGui.SameLine();
					if (ImGui.ImageButton("imgY", _currentComparison.ImageY, new Vector2(512, 512)))
					{
						_comparer.InsertComparison(_currentComparison.Key.Y, _currentComparison.Key.X);
						TrySort();
					}
				}
			}
		}

		ImGui.End();
	}

	private string[] InitComparisonMatrix()
	{
		if (_filenames == null)
			return Array.Empty<string>();
		
		_comparer = new MonkeyComparer();
		
		var path = Path.GetDirectoryName(_filenames[0]);
		var sortParams = Path.Combine(path, "sort.mky");
		if (!File.Exists(sortParams))
			return Array.Empty<string>();

		using var f = File.OpenRead(sortParams);
		var br = new BinaryReader(f);

		var loadedFiles = new List<string>();
		
		var numFiles = br.ReadInt32();
		for (var i = 0; i < numFiles; i++) 
			loadedFiles.Add(br.ReadString());

		var numKeys = br.ReadInt32();
		for (var i = 0; i < numKeys; i++)
		{
			var x = br.ReadString();
			var y = br.ReadString();
			var value = br.ReadInt32();

			_comparer.ComparisonMatrix[new ComparisonKey(x, y)] = value;
		}

		return loadedFiles.ToArray();
	}

	private void TrySort()
	{
		if (_filenames == null || _filenames.Count == 0)
			return;

		var path = Path.GetDirectoryName(_filenames[0]);
		using (var f = File.OpenWrite(Path.Combine(path, "sort.mky")))
		{
			var bw = new BinaryWriter(f);
			
			bw.Write(_filenames.Count);
			foreach (var filename in _filenames)
				bw.Write(filename);

			bw.Write(_comparer.ComparisonMatrix.Count);
			foreach (var (k, v) in _comparer.ComparisonMatrix)
			{
				bw.Write(k.X);
				bw.Write(k.Y);
				bw.Write(v);
			}
		}

		try
		{
			var array = MergeSort.Sort(_filenames, _comparer);
			SortComplete(array.ToArray());
		}
		catch (UndefinedComparisonException e)
		{
			_currentComparison = new ComparisonImages(e.Key, UploadImage(e.Key.X), UploadImage(e.Key.Y));
        }
	}

	private void SortComplete(string[] array)
	{
		for (var i = 0; i < array.Length; i++)
		{
			var dir = Path.GetDirectoryName(array[i]);
			var filename = Path.GetFileName(array[i]);

			var subdir = Path.Combine(dir, "sorted");
			Directory.CreateDirectory(subdir);
			
			File.Copy(array[i], Path.Combine(subdir, $"{i:0000}_{filename}"));
		}
	}

	private int UploadImage(string filename)
	{
		if (_imageHandles.ContainsKey(filename))
			return _imageHandles[filename];
		
		var image = Image.Load<Rgba32>(filename);
		image.Mutate(x =>
		{
			var size = x.GetCurrentSize();
			var longestSide = Math.Max(size.Width, size.Height);
			x.Resize(new ResizeOptions
			{
				Mode = ResizeMode.Pad,
				Size = new Size(longestSide, longestSide),
				PadColor = Color.Transparent
			});
		});
		
		var pixels = new byte[4 * image.Width * image.Height];
		image.CopyPixelDataTo(pixels);

		var tex = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, tex);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
		GL.BindTexture(TextureTarget.Texture2D, 0);

		_imageHandles[filename] = tex;

		return tex;
	}
}

public class Program
{
	class LoggingComparer : IComparer<int>
	{
		private HashSet<ComparisonKey> _uniqueKeys = new();
		public int UniqueComparisons = 0;
		
		public int Compare(int x, int y)
		{
			var a = new ComparisonKey($"{x}", $"{y}");
			var b = new ComparisonKey($"{y}", $"{x}");

			if (!_uniqueKeys.Contains(a) && !_uniqueKeys.Contains(b))
				UniqueComparisons++;
			
			_uniqueKeys.Add(a);
			_uniqueKeys.Add(b);

			return x.CompareTo(y);
		}
	}
	
	public static void Main(string[] args)
	{
		// var wnd = new Window();
		// wnd.Run();

		var r = new Random();
		var bytes = new int[100];
		for (var i = 0; i < bytes.Length; i++)
			bytes[i] = r.Next();

		var c = new LoggingComparer();
		var sorted = MergeSort.Sort(bytes.ToList(), c);
		Console.WriteLine(c.UniqueComparisons);
	}
}