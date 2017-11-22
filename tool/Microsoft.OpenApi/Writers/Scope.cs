﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi.Writers
{
    /// <summary>
    /// Various scope types for Open API writer.
    /// </summary>
    public enum ScopeType
    {
        /// <summary>
        /// Object scope.
        /// </summary>
        Object = 0,

        /// <summary>
        /// Array scope.
        /// </summary>
        Array = 1,
    }

    /// <summary>
    /// Class representing scope information.
    /// </summary>
    public sealed class Scope
    {
        /// <summary>
        /// The type of the scope.
        /// </summary>
        private readonly ScopeType type;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">The type of the scope.</param>
        public Scope(ScopeType type)
        {
            this.type = type;
        }

        /// <summary>
        /// Get/Set the object count for this scope.
        /// </summary>
        public int ObjectCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the scope type for this scope.
        /// </summary>
        public ScopeType Type
        {
            get
            {
                return this.type;
            }
        }

        /// <summary>
        /// Get/Set the whether it is in previous array scope.
        /// </summary>
        public bool IsInArray { get; set; } = false;
    }
}