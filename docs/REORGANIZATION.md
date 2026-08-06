# Repository Reorganization Decision

## Decision

Adopt the identity **FoundationKit for .NET**, introduce a core-only root solution, make the root GitHub Pages site the FoundationKit Showcase, and label EntertainmentDocs as the first reference consumer.

## Why the entire source tree is not physically moved in the same change

The current paths are embedded in:

- two solutions;
- project references;
- package scripts;
- Docker build contexts and Dockerfiles;
- Compose files;
- Nginx configuration;
- Postman documentation;
- Visual Studio profiles;
- CI path filters;
- more than 9,700 lines of repository-reference documentation.

Moving all files at once would create a large path-only diff, reduce review quality, and risk breaking a runtime that already passed full-stack validation. Architectural separation does not require premature physical extraction.

## What changes now

- repository identity and README;
- core-only `FoundationKit.sln` at the root;
- clear navigation boundaries for core, reference consumer, templates, and Showcase;
- interactive FoundationKit Showcase as the root site;
- project-idea issue form;
- CI validation for Showcase assets and the root core solution;
- package metadata prepared for the planned repository name;
- current documentation index updated.

## Future physical extraction

A later dedicated migration may move `platform/core/` to a new physical location or separate repository after package-based consumption is proven. That migration must preserve history where practical and update all project, Docker, CI, script, and documentation paths atomically.

## Non-goals

This reorganization does not:

- change EntertainmentDocs runtime behavior;
- change API contracts;
- change EF Core migrations or database schema;
- fix production-hardening gaps;
- convert the architecture to microservices;
- make FoundationKit an executable application.
