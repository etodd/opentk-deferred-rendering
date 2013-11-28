using System;
using System.Diagnostics;
using System.Drawing;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Lab5
{
	public class SimpleFBO : GameWindow
	{
		const int TextureSize = 512;
		
		public SimpleFBO()
			: base(TextureSize, TextureSize)
		{
		}

		uint ColorTexture;
		uint NormalTexture;
		uint DepthTexture;
		uint DeferredFBOHandle;
		
		uint LightingTexture;
		uint LightingFBOHandle;

		int cubeVS;
		int cubePS;
		int cubeProgram;
		
		int lightingVS;
		int lightingPS;
		int lightingProgram;
		
		int compositeVS;
		int compositePS;
		int compositeProgram;
		
		int CubeTexture;
		
		private int loadTexture(string filename)
		{
			int id = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, id);
			
			Bitmap bmp = new Bitmap(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), filename));
			System.Drawing.Imaging.BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);
			
			bmp.UnlockBits(bmp_data);
			
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
			
			return id;
		}
		
		class PointLight
		{
			public Vector3 Position = Vector3.Zero;
			public Vector3 Color = Vector3.One;
			public float Radius = 0.5f;
		}
		
		List<PointLight> pointLights = new List<PointLight>();

		private int compileShader(ShaderType type, string file)
		{
			int shader = GL.CreateShader(type);
			string errorInfo;
			int errorCode;
			
			using (StreamReader source = new StreamReader(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), file)))
			{
				string data = source.ReadToEnd();
				GL.ShaderSource(shader, data);
			}
				
			GL.CompileShader(shader);
			GL.GetShaderInfoLog(shader, out errorInfo);
			GL.GetShader(shader, ShaderParameter.CompileStatus, out errorCode);

			if (errorCode != 1)
				throw new ApplicationException(errorInfo);

			return shader;
		}

		private int compileShaderProgram(int vertexShader, int pixelShader)
		{
			int program = GL.CreateProgram();
			GL.AttachShader(program, pixelShader);
			GL.AttachShader(program, vertexShader);

			GL.LinkProgram(program);

			return program;
		}
		
		private bool showBuffers = false;
		
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == ' ')
				this.showBuffers = !this.showBuffers;
		}

		protected override void OnLoad(EventArgs e)
		{
			if (!GL.GetString(StringName.Extensions).Contains("EXT_framebuffer_object"))
			{
				System.Windows.Forms.MessageBox.Show
				(
					"Your video card does not support Framebuffer Objects. Please update your drivers.",
					"FBOs not supported",
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation
				);
				Exit();
			}
			
			// Add random point lights
			const float radius = 0.9f;
			Random random = new Random();
			for (int i = 0; i < 100; i++)
			{
				Vector3 color = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
				color.Normalize();
				float horizontalAngle = (float)random.NextDouble() * (float)Math.PI * 2.0f;
				float verticalAngle = (float)random.NextDouble() * (float)Math.PI;
				pointLights.Add(new PointLight
				{
					Position = new Vector3((float)Math.Sin(horizontalAngle) * (float)Math.Sin(verticalAngle), (float)Math.Cos(verticalAngle), (float)Math.Cos(horizontalAngle) * (float)Math.Sin(verticalAngle)) * radius,
					Color = color,
					Radius = 0.1f + (float)random.NextDouble() * 0.7f,
				});
			}
				
			GL.Enable(EnableCap.DepthTest);
			GL.ClearDepth(1.0f);
			GL.DepthFunc(DepthFunction.Lequal);
			
			this.CubeTexture = this.loadTexture("Textures/crate.jpg");

			// Create Color Tex
			GL.GenTextures(1, out ColorTexture);
			GL.BindTexture(TextureTarget.Texture2D, ColorTexture);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, TextureSize, TextureSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
			
			// Create normal tex
			GL.GenTextures(1, out NormalTexture);
			GL.BindTexture(TextureTarget.Texture2D, NormalTexture);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, TextureSize, TextureSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

			// Create Depth Tex
			GL.GenTextures(1, out DepthTexture);
			GL.BindTexture(TextureTarget.Texture2D, DepthTexture);
			GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)All.R32f, TextureSize, TextureSize, 0, PixelFormat.Red, PixelType.UnsignedInt, IntPtr.Zero);
			// things go horribly wrong if DepthComponent's Bitcount does not match the main Framebuffer's Depth
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
			
			// Create internal depth buffer
			uint internalDepthBuffer;
			GL.GenTextures(1, out internalDepthBuffer);
			GL.BindTexture(TextureTarget.Texture2D, internalDepthBuffer);
			GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)All.DepthComponent32, TextureSize, TextureSize, 0, PixelFormat.DepthComponent, PixelType.UnsignedInt, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
			
			// Create a FBO and attach the textures
			GL.Ext.GenFramebuffers(1, out DeferredFBOHandle);
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, DeferredFBOHandle);
			GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.Texture2D, ColorTexture, 0);
			GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment1Ext, TextureTarget.Texture2D, NormalTexture, 0);
			GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment2Ext, TextureTarget.Texture2D, DepthTexture, 0);
			GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, internalDepthBuffer, 0);
			
			// Create the lighting buffer
			GL.GenTextures(1, out LightingTexture);
			GL.BindTexture(TextureTarget.Texture2D, LightingTexture);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, TextureSize, TextureSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
			GL.Ext.GenFramebuffers(1, out LightingFBOHandle);
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, LightingFBOHandle);
			GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.Texture2D, LightingTexture, 0);
			
			GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);

			this.cubeVS = this.compileShader(ShaderType.VertexShader, "Shaders/CubeVS.glsl");
			this.cubePS = this.compileShader(ShaderType.FragmentShader, "Shaders/CubePS.glsl");
			this.cubeProgram = this.compileShaderProgram(this.cubeVS, this.cubePS);
			
			this.lightingVS = this.compileShader(ShaderType.VertexShader, "Shaders/LightingVS.glsl");
			this.lightingPS = this.compileShader(ShaderType.FragmentShader, "Shaders/LightingPS.glsl");
			this.lightingProgram = this.compileShaderProgram(this.lightingVS, this.lightingPS);
			
			this.compositeVS = this.compileShader(ShaderType.VertexShader, "Shaders/CompositeVS.glsl");
			this.compositePS = this.compileShader(ShaderType.FragmentShader, "Shaders/CompositePS.glsl");
			this.compositeProgram = this.compileShaderProgram(this.compositeVS, this.compositePS);

			#region Test for Error

			switch (GL.Ext.CheckFramebufferStatus(FramebufferTarget.FramebufferExt))
			{
				case FramebufferErrorCode.FramebufferCompleteExt:
					{
						Console.WriteLine("FBO: The framebuffer is complete and valid for rendering.");
						break;
					}
				case FramebufferErrorCode.FramebufferIncompleteAttachmentExt:
					{
						Console.WriteLine("FBO: One or more attachment points are not framebuffer attachment complete. This could mean there’s no texture attached or the format isn’t renderable. For color textures this means the base format must be RGB or RGBA and for depth textures it must be a DEPTH_COMPONENT format. Other causes of this error are that the width or height is zero or the z-offset is out of range in case of render to volume.");
						break;
					}
				case FramebufferErrorCode.FramebufferIncompleteMissingAttachmentExt:
					{
						Console.WriteLine("FBO: There are no attachments.");
						break;
					}
				/* case  FramebufferErrorCode.GL_FRAMEBUFFER_INCOMPLETE_DUPLICATE_ATTACHMENT_EXT: 
					 {
						 Console.WriteLine("FBO: An object has been attached to more than one attachment point.");
						 break;
					 }*/
				case FramebufferErrorCode.FramebufferIncompleteDimensionsExt:
					{
						Console.WriteLine("FBO: Attachments are of different size. All attachments must have the same width and height.");
						break;
					}
				case FramebufferErrorCode.FramebufferIncompleteFormatsExt:
					{
						Console.WriteLine("FBO: The color attachments have different format. All color attachments must have the same format.");
						break;
					}
				case FramebufferErrorCode.FramebufferIncompleteDrawBufferExt:
					{
						Console.WriteLine("FBO: An attachment point referenced by GL.DrawBuffers() doesn’t have an attachment.");
						break;
					}
				case FramebufferErrorCode.FramebufferIncompleteReadBufferExt:
					{
						Console.WriteLine("FBO: The attachment point referenced by GL.ReadBuffers() doesn’t have an attachment.");
						break;
					}
				case FramebufferErrorCode.FramebufferUnsupportedExt:
					{
						Console.WriteLine("FBO: This particular FBO configuration is not supported by the implementation.");
						break;
					}
				default:
					{
						Console.WriteLine("FBO: Status unknown. (yes, this is really bad.)");
						break;
					}
			}

			// using FBO might have changed states, e.g. the FBO might not support stereoscopic views or double buffering
			int[] queryinfo = new int[6];
			GL.GetInteger(GetPName.MaxColorAttachmentsExt, out queryinfo[0]);
			GL.GetInteger(GetPName.AuxBuffers, out queryinfo[1]);
			GL.GetInteger(GetPName.MaxDrawBuffers, out queryinfo[2]);
			GL.GetInteger(GetPName.Stereo, out queryinfo[3]);
			GL.GetInteger(GetPName.Samples, out queryinfo[4]);
			GL.GetInteger(GetPName.Doublebuffer, out queryinfo[5]);
			Console.WriteLine("max. ColorBuffers: " + queryinfo[0] + " max. AuxBuffers: " + queryinfo[1] + " max. DrawBuffers: " + queryinfo[2] +
							   "\nStereo: " + queryinfo[3] + " Samples: " + queryinfo[4] + " DoubleBuffer: " + queryinfo[5]);

			Console.WriteLine("Last GL Error: " + GL.GetError());

			#endregion Test for Error
		}

		protected override void OnUnload(EventArgs e)
		{
			// Clean up what we allocated before exiting
			if (ColorTexture != 0)
				GL.DeleteTextures(1, ref ColorTexture);

			if (DepthTexture != 0)
				GL.DeleteTextures(1, ref DepthTexture);
			
			if (NormalTexture != 0)
				GL.DeleteTextures(1, ref NormalTexture);

			if (DeferredFBOHandle != 0)
				GL.Ext.DeleteFramebuffers(1, ref DeferredFBOHandle);
		}

		private Matrix4 projectionMatrix;

		protected override void OnResize(EventArgs e)
		{
			GL.Viewport(0, 0, Width, Height);

			double aspect_ratio = Width / (double)Height;

			this.projectionMatrix = OpenTK.Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, (float)aspect_ratio, 1, 64);

			base.OnResize(e);
		}
		
		float angle = 0.0f;

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			
			angle += (float)e.Time;

			if (Keyboard[Key.Escape])
				this.Exit();
		}

		private Matrix4 viewMatrix;
		
		private void sphereVertex(int horizontal, int vertical, float horizontalInterval, float verticalInterval, float radius)
		{
			float horizontalAngle = horizontal * horizontalInterval, verticalAngle = vertical * verticalInterval;
			Vector3 normal = new Vector3((float)Math.Sin(horizontalAngle) * (float)Math.Sin(verticalAngle), (float)Math.Cos(verticalAngle), (float)Math.Cos(horizontalAngle) * (float)Math.Sin(verticalAngle));
			GL.Normal3(normal);
			GL.Vertex3(normal * radius);
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			const float farPlane = 6.0f;
			Vector3 cameraPosition = new Vector3(0, 1, 3);
			this.viewMatrix = OpenTK.Matrix4.LookAt(cameraPosition, Vector3.Zero, Vector3.UnitY);
			Matrix4 viewProjection = this.viewMatrix * this.projectionMatrix;
			
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, DeferredFBOHandle);
			GL.PushAttrib(AttribMask.ViewportBit);
			{
				GL.Viewport(0, 0, TextureSize, TextureSize);

				DrawBuffersEnum[] buffers = new[]
				{
					(DrawBuffersEnum)FramebufferAttachment.ColorAttachment0Ext,
					(DrawBuffersEnum)FramebufferAttachment.ColorAttachment1Ext,
					(DrawBuffersEnum)FramebufferAttachment.ColorAttachment2Ext,
				};

				GL.DrawBuffers(buffers.Length, buffers);
				
				GL.ClearColor(0f, 0f, 0f, 0f);
				GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				
				Matrix4 world = Matrix4.CreateRotationY(this.angle);
				
				GL.UseProgram(this.cubeProgram);
				GL.UniformMatrix4(GL.GetUniformLocation(this.cubeProgram, "ViewProjectionMatrix"), false, ref viewProjection);
				GL.UniformMatrix4(GL.GetUniformLocation(this.cubeProgram, "WorldMatrix"), false, ref world);
				GL.Uniform3(GL.GetUniformLocation(this.cubeProgram, "CameraPosition"), ref cameraPosition);
				GL.Uniform1(GL.GetUniformLocation(this.cubeProgram, "FarPlane"), farPlane);
				
				GL.ActiveTexture(TextureUnit.Texture0);
				GL.BindTexture(TextureTarget.Texture2D, CubeTexture);
				GL.Uniform1(GL.GetUniformLocation(this.lightingProgram, "DiffuseTexture"), 0);
				
				GL.CullFace(CullFaceMode.Back);
				GL.Begin(BeginMode.Quads);
				{
					const float size = 0.5f;

					// Back
					GL.Normal3(0, 0, -1);
					GL.TexCoord2(0, 0);
					GL.Vertex3(-size, -size, -size);
					GL.TexCoord2(0, 1);
					GL.Vertex3(-size, size, -size);
					GL.TexCoord2(1, 1);
					GL.Vertex3(size, size, -size);
					GL.TexCoord2(1, 0);
					GL.Vertex3(size, -size, -size);

					// Front
					GL.Normal3(0, 0, 1);
					GL.TexCoord2(0, 0);
					GL.Vertex3(-size, -size, size);
					GL.TexCoord2(1, 0);
					GL.Vertex3(size, -size, size);
					GL.TexCoord2(1, 1);
					GL.Vertex3(size, size, size);
					GL.TexCoord2(0, 1);
					GL.Vertex3(-size, size, size);

					// Left
					GL.Normal3(-1, 0, 0);
					GL.TexCoord2(0, 1);
					GL.Vertex3(-size, -size, size);
					GL.TexCoord2(1, 1);
					GL.Vertex3(-size, size, size);
					GL.TexCoord2(1, 0);
					GL.Vertex3(-size, size, -size);
					GL.TexCoord2(0, 0);
					GL.Vertex3(-size, -size, -size);

					// Right
					GL.Normal3(1, 0, 0);
					GL.TexCoord2(0, 0);
					GL.Vertex3(size, -size, -size);
					GL.TexCoord2(0, 1);
					GL.Vertex3(size, size, -size);
					GL.TexCoord2(1, 1);
					GL.Vertex3(size, size, size);
					GL.TexCoord2(1, 0);
					GL.Vertex3(size, -size, size);

					// Top
					GL.Normal3(0, 1, 0);
					GL.TexCoord2(0, 1);
					GL.Vertex3(-size, size, size);
					GL.TexCoord2(1, 1);
					GL.Vertex3(size, size, size);
					GL.TexCoord2(1, 0);
					GL.Vertex3(size, size, -size);
					GL.TexCoord2(0, 0);
					GL.Vertex3(-size, size, -size);

					// Bottom
					GL.Normal3(0, -1, 0);
					GL.TexCoord2(0, 0);
					GL.Vertex3(-size, -size, -size);
					GL.TexCoord2(0, 1);
					GL.Vertex3(size, -size, -size);
					GL.TexCoord2(1, 1);
					GL.Vertex3(size, -size, size);
					GL.TexCoord2(0, 1);
					GL.Vertex3(-size, -size, size);
				}
				GL.End();
				GL.UseProgram(0);
			}
			GL.PopAttrib();
			
			// Render lighting buffer
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, LightingFBOHandle);
			GL.PushAttrib(AttribMask.ViewportBit);
			{
				GL.Viewport(0, 0, TextureSize, TextureSize);
				
				GL.ClearColor(0f, 0f, 0f, 0f);
				GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				
				GL.PolygonMode(MaterialFace.Back, PolygonMode.Fill);
				GL.Disable(EnableCap.DepthTest);
				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
				GL.UseProgram(this.lightingProgram);
				GL.UniformMatrix4(GL.GetUniformLocation(this.lightingProgram, "ViewProjectionMatrix"), false, ref viewProjection);
				GL.Uniform3(GL.GetUniformLocation(this.lightingProgram, "CameraPosition"), ref cameraPosition);
				GL.Uniform1(GL.GetUniformLocation(this.lightingProgram, "FarPlane"), farPlane);
				
				// Read from the normal and depth textures
				GL.ActiveTexture(TextureUnit.Texture0);
				GL.BindTexture(TextureTarget.Texture2D, DepthTexture);
				GL.Uniform1(GL.GetUniformLocation(this.lightingProgram, "DepthBuffer"), 0);
 
				GL.ActiveTexture(TextureUnit.Texture1);
				GL.BindTexture(TextureTarget.Texture2D, NormalTexture);
				GL.Uniform1(GL.GetUniformLocation(this.lightingProgram, "NormalBuffer"), 1);
				
				// Display the back faces only. This prevents issues when the camera is inside a point light.
				GL.CullFace(CullFaceMode.Front);
				
				foreach (PointLight light in this.pointLights)
				{
					GL.Uniform3(GL.GetUniformLocation(this.lightingProgram, "Color"), ref light.Color);
					GL.Uniform3(GL.GetUniformLocation(this.lightingProgram, "Position"), ref light.Position);
					GL.Uniform1(GL.GetUniformLocation(this.lightingProgram, "Radius"), light.Radius);
					Matrix4 world = Matrix4.CreateTranslation(light.Position);
					GL.UniformMatrix4(GL.GetUniformLocation(this.lightingProgram, "WorldMatrix"), false, ref world);
					
					GL.Begin(BeginMode.Quads);
					{
						const int verticalSegments = 16, horizontalSegments = 16;
						const float verticalInterval = (float)Math.PI / (float)verticalSegments, horizontalInterval = ((float)Math.PI * 2.0f) / (float)horizontalSegments;
						for (int vertical = 0; vertical < verticalSegments; vertical++)
						{
							for (int horizontal = 0; horizontal < horizontalSegments; horizontal++)
							{
								this.sphereVertex(horizontal, vertical, horizontalInterval, verticalInterval, light.Radius);
								this.sphereVertex(horizontal, vertical + 1, horizontalInterval, verticalInterval, light.Radius);
								this.sphereVertex(horizontal + 1, vertical + 1, horizontalInterval, verticalInterval, light.Radius);
								this.sphereVertex(horizontal + 1, vertical, horizontalInterval, verticalInterval, light.Radius);
							}
						}
					}
					GL.End();
				}
				GL.UseProgram(0);
				GL.Disable(EnableCap.Blend);
				GL.Enable(EnableCap.DepthTest);
				GL.ActiveTexture(TextureUnit.Texture0);
			}
			GL.PopAttrib();
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0); // disable rendering into the FBO
			
			GL.ClearColor(0f, 0f, 0f, 0f);

			GL.Enable(EnableCap.Texture2D); // enable Texture Mapping
			GL.BindTexture(TextureTarget.Texture2D, 0); // bind default texture
			
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref Matrix4.Identity);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.CullFace(CullFaceMode.Back);
			
			if (this.showBuffers)
			{
				GL.PushMatrix();
				{
					// Draw the Color Texture
					GL.Scale(0.5f, 0.5f, 0.5f);
					GL.Translate(-1.0f, -1.0f, 0f);
					GL.BindTexture(TextureTarget.Texture2D, ColorTexture);
					GL.Begin(BeginMode.Quads);
					{
						GL.TexCoord2(0f, 1f);
						GL.Vertex2(-1.0f, 1.0f);
						GL.TexCoord2(0.0f, 0.0f);
						GL.Vertex2(-1.0f, -1.0f);
						GL.TexCoord2(1.0f, 0.0f);
						GL.Vertex2(1.0f, -1.0f);
						GL.TexCoord2(1.0f, 1.0f);
						GL.Vertex2(1.0f, 1.0f);
					}
					GL.End();
				}
				GL.PopMatrix();
				
				GL.PushMatrix();
				{
					// Draw the Depth Texture
					GL.Scale(0.5f, 0.5f, 0.5f);
					GL.Translate(1.0f, -1.0f, 0f);
					GL.BindTexture(TextureTarget.Texture2D, DepthTexture);
					GL.Begin(BeginMode.Quads);
					{
						GL.TexCoord2(0f, 1f);
						GL.Vertex2(-1.0f, 1.0f);
						GL.TexCoord2(0.0f, 0.0f);
						GL.Vertex2(-1.0f, -1.0f);
						GL.TexCoord2(1.0f, 0.0f);
						GL.Vertex2(1.0f, -1.0f);
						GL.TexCoord2(1.0f, 1.0f);
						GL.Vertex2(1.0f, 1.0f);
					}
					GL.End();
				}
				GL.PopMatrix();
				
				GL.PushMatrix();
				{
					// Draw the normal Texture
					GL.Scale(0.5f, 0.5f, 0.5f);
					GL.Translate(-1.0f, 1.0f, 0f);
					GL.BindTexture(TextureTarget.Texture2D, NormalTexture);
					GL.Begin(BeginMode.Quads);
					{
						GL.TexCoord2(0f, 1f);
						GL.Vertex2(-1.0f, 1.0f);
						GL.TexCoord2(0.0f, 0.0f);
						GL.Vertex2(-1.0f, -1.0f);
						GL.TexCoord2(1.0f, 0.0f);
						GL.Vertex2(1.0f, -1.0f);
						GL.TexCoord2(1.0f, 1.0f);
						GL.Vertex2(1.0f, 1.0f);
					}
					GL.End();
				}
				GL.PopMatrix();
				
				GL.PushMatrix();
				{
					// Draw the normal Texture
					GL.Scale(0.5f, 0.5f, 0.5f);
					GL.Translate(1.0f, 1.0f, 0f);
					GL.BindTexture(TextureTarget.Texture2D, LightingTexture);
					GL.Begin(BeginMode.Quads);
					{
						GL.TexCoord2(0f, 1f);
						GL.Vertex2(-1.0f, 1.0f);
						GL.TexCoord2(0.0f, 0.0f);
						GL.Vertex2(-1.0f, -1.0f);
						GL.TexCoord2(1.0f, 0.0f);
						GL.Vertex2(1.0f, -1.0f);
						GL.TexCoord2(1.0f, 1.0f);
						GL.Vertex2(1.0f, 1.0f);
					}
					GL.End();
				}
				GL.PopMatrix();
			}
			else
			{
				// Render composite
				GL.UseProgram(this.compositeProgram);
				GL.ActiveTexture(TextureUnit.Texture0);
				GL.BindTexture(TextureTarget.Texture2D, ColorTexture);
				GL.Uniform1(GL.GetUniformLocation(this.compositeProgram, "ColorBuffer"), 0);
 
				GL.ActiveTexture(TextureUnit.Texture1);
				GL.BindTexture(TextureTarget.Texture2D, LightingTexture);
				GL.Uniform1(GL.GetUniformLocation(this.compositeProgram, "LightingBuffer"), 1);
				
				GL.Begin(BeginMode.Quads);
				{
					GL.TexCoord2(0f, 1f);
					GL.Vertex2(-1.0f, 1.0f);
					GL.TexCoord2(0.0f, 0.0f);
					GL.Vertex2(-1.0f, -1.0f);
					GL.TexCoord2(1.0f, 0.0f);
					GL.Vertex2(1.0f, -1.0f);
					GL.TexCoord2(1.0f, 1.0f);
					GL.Vertex2(1.0f, 1.0f);
				}
				GL.End();
			}
			
			this.SwapBuffers();
		}

		#region public static void Main()

		/// <summary>
		/// Entry point of this example.
		/// </summary>
		[STAThread]
		public static void Main()
		{
			using (SimpleFBO example = new SimpleFBO())
				example.Run(30.0, 0.0);
		}

		#endregion
	}
}