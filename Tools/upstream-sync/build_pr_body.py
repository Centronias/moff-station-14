#!/usr/bin/env python3
"""Write the body for the upstream-sync PR.

Must be run with HEAD checked out on the sync branch, after the clean range
has already been merged in (so `HEAD` is the real merge commit, and a
merge-tree trial against it reflects what a contributor would actually hit
next).
"""
import subprocess
import sys


def run(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run(args, capture_output=True, text=True)


def conflicting_files(conflict_sha: str) -> list[str]:
    """Names of the files that conflict between HEAD and conflict_sha.

    `git merge-tree --write-tree --name-only` prints the resulting tree's
    OID on the first line, then (on conflict) the conflicted paths, then a
    blank line, then human-readable messages.
    """
    result = run("git", "merge-tree", "--write-tree", "--name-only", "HEAD", conflict_sha)
    lines = result.stdout.splitlines()[1:]
    files = []
    for line in lines:
        if not line:
            break
        files.append(line)
    return files


def main() -> None:
    if len(sys.argv) != 5:
        sys.exit("usage: build_pr_body.py <base-ref> <clean-sha> <conflict-sha-or-empty> <out-file>")
    base_ref, clean_sha, conflict_sha, out_file = sys.argv[1:5]

    commit_count = run("git", "rev-list", "--count", f"{base_ref}..{clean_sha}").stdout.strip()
    file_count = len([
        line for line in run("git", "diff", "--name-only", base_ref, clean_sha).stdout.splitlines() if line
    ])

    lines = [
        "Automated merge of upstream commits that apply cleanly. This PR is "
        "force-pushed in place on every run of the `Upstream Sync` workflow, "
        "so it stays up to date rather than piling up duplicates.",
        "",
        f"- Upstream range merged: `{base_ref}..{clean_sha}`",
        f"- Commits: {commit_count}",
        f"- Files changed: {file_count}",
        "",
    ]

    if conflict_sha:
        subject = run("git", "log", "-1", "--format=%s", conflict_sha).stdout.strip()
        author = run("git", "log", "-1", "--format=%an", conflict_sha).stdout.strip()
        lines += [
            "## Stopped before a conflicting commit",
            "",
            "The next upstream commit does not merge cleanly and was left out of this PR:",
            "",
            f"- `{conflict_sha}` by {author}: {subject}",
            "",
            "Conflicting files:",
            "",
        ]
        lines += [f"- `{path}`" for path in conflicting_files(conflict_sha)]
        lines += [
            "",
            "Resolve that commit manually (merge/cherry-pick it onto this branch and "
            "fix the conflicts) to continue the sync; the next scheduled run will pick "
            "up from wherever `master` ends up.",
        ]
    else:
        lines.append(f"This PR is fully caught up with upstream as of `{clean_sha}`.")

    with open(out_file, "w", newline="\n") as f:
        f.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()
