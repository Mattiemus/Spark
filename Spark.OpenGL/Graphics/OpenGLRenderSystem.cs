﻿namespace Spark.Graphics.Renderer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using Core;
    using Math;
    using Utilities;
    using Implementation;

    using OGL = OpenTK.Graphics.OpenGL;

    /// <summary>
    /// OpenGL render system implementation
    /// </summary>
    public sealed class OpenGLRenderSystem : BaseDisposable, IRenderSystem
    {
        private readonly ImplementationFactoryCollection _implementationFactories;
        private readonly VertexArrayObject _vao;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenGLRenderSystem"/> class
        /// </summary>
        public OpenGLRenderSystem()
        {
            _implementationFactories = new ImplementationFactoryCollection();

            State = new OpenGLState();

            _vao = new VertexArrayObject();
            OGL.GL.BindVertexArray(_vao.ResourceId);
        }

        /// <summary>
        /// Gets the identifier that describes the render system platform.
        /// </summary>
        public string Platform => "OpenGL 3.2";

        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        public string Name => "OpenGL Render System";

        /// <summary>
        /// Gets the state manager instance
        /// </summary>
        public OpenGLState State { get; }

        /// <summary>
        /// Initializes the service. This is called by the engine when a service is newly registered.
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void Initialize(Engine engine)
        {
            // No-op
        }

        /// <summary>
        /// Gets the implementation factory of the specified type.
        /// </summary>
        /// <typeparam name="T">Implementation factory type</typeparam>
        /// <returns>The registered implementation factory, if it exists. Otherwise, null is returned.</returns>
        public T GetImplementationFactory<T>() where T : IGraphicsResourceImplementationFactory
        {
            return _implementationFactories.GetImplementationFactory<T>();
        }

        /// <summary>
        /// Tries to get the implementation factory of the specified type.
        /// </summary>
        /// <typeparam name="T">Implementation factory type</typeparam>
        /// <param name="implementationFactory">The registered implementation factory, if it exists.</param>
        /// <returns>True if the factory was registered and found, false otherwise.</returns>
        public bool TryGetImplementationFactory<T>(out T implementationFactory) where T : IGraphicsResourceImplementationFactory
        {
            return _implementationFactories.TryGetImplementationFactory<T>(out implementationFactory);
        }

        /// <summary>
        /// Queries if the graphics resource type (e.g. VertexBuffer) is supported by any of the registered implementation factories.
        /// </summary>
        /// <typeparam name="T">Graphics resource type</typeparam>
        /// <returns>True if the type is supported by an implementation factory, false otherwise.</returns>
        public bool IsSupported<T>() where T : GraphicsResource
        {
            return _implementationFactories.IsSupported<T>();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        public IEnumerator<IGraphicsResourceImplementationFactory> GetEnumerator()
        {
            return _implementationFactories.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _implementationFactories.GetEnumerator();
        }






















        /// <summary>
        /// Clears all bounded render targets to the specified color
        /// </summary>
        /// <param name="color">Color to clear to</param>
        public void Clear(LinearColor color)
        {
            Clear(ClearOptions.All, color, 1.0f, 0);
        }

        /// <summary>
        /// Clears all bounded render targets and depth buffer.
        /// </summary>
        /// <param name="options">Clear options specifying which buffer to clear.</param>
        /// <param name="color">Color to clear to</param>
        /// <param name="depth">Depth value to clear to</param>
        /// <param name="stencil">Stencil value to clear to</param>
        public void Clear(ClearOptions options, LinearColor color, float depth, int stencil)
        {
            State.ColorBuffer.ClearValue = color;
            State.DepthBuffer.ClearValue = depth;
            State.StencilBuffer.ClearValue = stencil;
            OGL.GL.Clear(GraphicsHelpers.ToNative(options));
        }

        /// <summary>
        /// Draws non-indexed, non-instanced geometry.
        /// </summary>
        /// <param name="primitiveType">Type of primitives to draw.</param>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexIndex">Starting index in a vertex buffer at which to read vertices from.</param>
        public void Draw(PrimitiveType primitiveType, int vertexCount, int startVertexIndex)
        {
            OGL.GL.DrawArrays(GraphicsHelpers.ToNative(primitiveType), vertexCount, startVertexIndex);
        }







        /// <summary>
        /// Performs the dispose action
        /// </summary>
        /// <param name="isDisposing">True if called from dispose, false if called from the finalizer</param>
        protected override void DisposeInternal(bool isDisposing)
        {
        }
    }
}