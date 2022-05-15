﻿using System.Diagnostics.CodeAnalysis;
using Vernuntii.SemVer;

namespace Vernuntii.VersionTransformers
{
    /// <summary>
    /// Extension methods for 
    /// </summary>
    public static class SemanticVersionTransformerExtensions
    {
        /// <summary>
        /// Checks <paramref name="transformer"/> is not null and can transform.
        /// </summary>
        /// <param name="transformer"></param>
        public static bool CanTransform([NotNullWhen(true)] this ISemanticVersionTransformer? transformer) =>
            transformer is not null && !transformer.DoesNotTransform;

        /// <summary>
        /// Transforms the version.
        /// </summary>
        /// <param name="versionTransformer"></param>
        /// <param name="version"></param>
        public static ISemanticVersion TransformVersion(this ISemanticVersionTransformer versionTransformer, SemanticVersion version) =>
            versionTransformer.TransformVersion(version);

        /// <summary>
        /// Transforms the version.
        /// </summary>
        /// <param name="versionTransformers"></param>
        /// <param name="startVersion"></param>
        public static ISemanticVersion TransformVersion(this IEnumerable<ISemanticVersionTransformer> versionTransformers, ISemanticVersion startVersion)
        {
            var preflightVersion = startVersion;

            foreach (var versionTransformer in versionTransformers) {
                if (versionTransformer is null || versionTransformer.DoesNotTransform) {
                    continue;
                }

                preflightVersion = versionTransformer.TransformVersion(preflightVersion);
            }

            return preflightVersion;
        }
    }
}
