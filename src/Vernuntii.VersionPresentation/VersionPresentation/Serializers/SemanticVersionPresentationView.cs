﻿namespace Vernuntii.VersionPresentation.Serializers
{
    /// <summary>
    /// The view of presentation.
    /// </summary>
    public enum SemanticVersionPresentationView
    {
        /// <summary>
        /// Uses textual representation.
        /// </summary>
        Text,
        /// <summary>
        /// Uses JSON serializer.
        /// </summary>
        Json,
        /// <summary>
        /// Uses YAML serializer.
        /// </summary>
        Yaml,
    }
}
