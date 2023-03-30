using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Diagnostics;

namespace VisualMonkeySort;

public abstract class ImGuiWindow : GameWindow
{
	ImGuiController _controller;

	public ImGuiWindow() : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = new Vector2i(1280, 720), APIVersion = new Version(3, 3) })
	{
	}

	protected override void OnLoad()
	{
		base.OnLoad();

		_controller = new ImGuiController(ClientSize.X, ClientSize.Y);
	}

	protected override void OnResize(ResizeEventArgs e)
	{
		base.OnResize(e);

		// Update the opengl viewport
		GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

		// Tell ImGui of the new size
		_controller.WindowResized(ClientSize.X, ClientSize.Y);
	}

	protected override void OnRenderFrame(FrameEventArgs e)
	{
		base.OnRenderFrame(e);

		_controller.Update(this, (float)e.Time);

		PreClear();
		GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

		Process();

		_controller.Render();

		ImGuiController.CheckGLError("End of frame");

		SwapBuffers();
	}

	public virtual void PreClear()
	{
		GL.ClearColor(new Color4(0, 0, 0, 0));
	}

	public abstract void Process();

	protected override void OnTextInput(TextInputEventArgs e)
	{
		base.OnTextInput(e);
		_controller.PressChar((char)e.Unicode);
	}

	protected override void OnMouseWheel(MouseWheelEventArgs e)
	{
		base.OnMouseWheel(e);
		_controller.MouseScroll(e.Offset);
	}
}