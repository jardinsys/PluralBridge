Thanks for your interest in contributing.

## License for Contributions

By submitting a pull request, patch, or other contribution to this repository, you agree that your contribution is provided under the same license as this project:

GNU General Public License, version 3 or, at your option, any later version (GPL-3.0-or-later).

## Branch and Deployment Workflow

PluralBridge uses a small branch structure so production stays stable, development stays reviewable, and project work can be previewed before it reaches users.

### Branch roles

#### master

`master` is the production branch.

Rules for `master`:

- `master` only gets merged from `dev`.
- `master` must never be in a broken state.
- `master` represents the current public release.

#### dev

`dev` is the stable integration branch for the next release.

Rules for `dev`:

- `dev` only gets merged from project branches.
- `dev` must never be in a broken state.
- `dev` is where reviewed project work is collected before release.

#### <project_branch>

A project branch is where active work happens.

Rules for project branches:

- Create project branches from `dev`.
- Keep each project branch focused on one coherent change.
- Test the work before merging it back into `dev`.
- Use clear branch names such as `feature/docs-rendering`, `feature/branching-workflow-docs`, or `patch/image-fix`.

### Development and Deployment Rules

#### Web development

For website and REST service work, use Cloudflare temporary branch previews so changes can be inspected before they merge forward.

**Needs follow-up:** document the exact Cloudflare branch-preview setup and naming rules once the project workflow is finalized.

#### Client application work

For Windows, Linux, macOS, Android, and iOS client work, the release workflow is still TBD.

The project will need an automated build process that runs when client code is committed and pushed.

### Contributor expectations

Contributors should keep changes small, reviewable, and safe.

Expected behavior:

- Start from the correct branch.
- Keep unrelated changes out of the same pull request.
- Do not commit generated exports, private data, credentials, local database files, or machine-specific artifacts.
- Include a clear summary of what changed and how it was tested.
- Update documentation when behavior, commands, website pages, or user-facing workflows change.

## Developer Notes

- Preserve existing copyright and license notices.
- Add a prominent notice to modified files when you make substantive changes.
- Keep changes focused and well-described.
- Update documentation when behavior changes.

## Pull Requests

Please include:

- a clear description of the change
- the reason for the change
- any testing performed

## Privacy and Safety

Do not include real exported Simply Plural data in pull requests, issues, examples, screenshots, test fixtures, or documentation unless it is your own data and you intentionally chose to make it public.

Avoid publishing:

- API tokens
- user IDs
- member names
- avatar images
- note contents
- friends lists
- fronting history
- custom fields
- privacy buckets
- screenshots containing private data

Use redacted examples and synthetic test data whenever possible.
