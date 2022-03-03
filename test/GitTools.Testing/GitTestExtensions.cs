﻿/* The MIT License (MIT)

Copyright (c) 2021 NServiceBus Ltd, GitTools and contributors.

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
*/
using System.Text;
using GitTools.Testing.Internal;
using LibGit2Sharp;

namespace GitTools.Testing;

public static class GitTestExtensions
{
    private static int _pad = 1;

    public static Commit MakeACommit(this IRepository repository, string? commitMessage = null) => CreateFileAndCommit(repository, Guid.NewGuid().ToString(), commitMessage);

    public static void MergeNoFF(this IRepository repository, string branch) => MergeNoFF(repository, branch, Generate.SignatureNow());

    public static void MergeNoFF(this IRepository repository, string branch, Signature sig) => repository.Merge(repository.Branches[branch], sig, new MergeOptions
    {
        FastForwardStrategy = FastForwardStrategy.NoFastForward
    });

    public static Commit[] MakeCommits(this IRepository repository, int numCommitsToMake) => Enumerable.Range(1, numCommitsToMake)
        .Select(_ => repository.MakeACommit())
        .ToArray();

    private static Commit CreateFileAndCommit(this IRepository repository, string relativeFileName, string? commitMessage = null)
    {
        var randomFile = Path.Combine(repository.Info.WorkingDirectory, relativeFileName);
        if (File.Exists(randomFile))
        {
            File.Delete(randomFile);
        }

        var totalWidth = 36 + (_pad++ % 10);
        var contents = Guid.NewGuid().ToString().PadRight(totalWidth, '.');
        File.WriteAllText(randomFile, contents);

        Commands.Stage(repository, randomFile);

        return repository.Commit(commitMessage ?? $"Test Commit for file '{relativeFileName}'",
            Generate.SignatureNow(), Generate.SignatureNow());
    }

    public static Tag MakeATaggedCommit(this IRepository repository, string tag)
    {
        var commit = repository.MakeACommit();
        var existingTag = repository.Tags.SingleOrDefault(t => t.FriendlyName == tag);
        return existingTag != null ? existingTag : repository.Tags.Add(tag, commit);
    }

    public static Commit CreatePullRequestRef(this IRepository repository, string from, string to, int prNumber = 2, bool normalise = false, bool allowFastForwardMerge = false)
    {
        Commands.Checkout(repository, repository.Branches[to].Tip);
        if (allowFastForwardMerge)
        {
            repository.Merge(repository.Branches[from], Generate.SignatureNow());
        }
        else
        {
            repository.MergeNoFF(from);
        }
        var commit = repository.Head.Tip;
        repository.Refs.Add("refs/pull/" + prNumber + "/merge", commit.Id);
        Commands.Checkout(repository, to);
        if (normalise)
        {
            // Turn the ref into a real branch
            Commands.Checkout(repository, repository.Branches.Add("pull/" + prNumber + "/merge", commit));
        }

        return commit;
    }

    public static void ExecuteGitCmd(string gitCmd, Action<string>? writer = null)
    {
        var output = new StringBuilder();
        try
        {
            ProcessHelper.Run(
                o => output.AppendLine(o),
                e => output.AppendLineFormat("ERROR: {0}", e),
                null,
                "git",
                gitCmd,
                ".");
        }
        catch (FileNotFoundException exception)
        {
            if (exception.FileName != "git")
            {
                throw;
            }

            output.AppendLine("Could not execute 'git log' due to the following error:");
            output.AppendLine(exception.ToString());
        }

        if (writer != null)
        {
            writer(output.ToString());
        }
        else
        {
            Console.Write(output.ToString());
        }
    }
}
