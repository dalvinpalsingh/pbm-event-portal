# PBM Event Portal

A web app for running Punjabi Bhasha Mela, a Punjabi language competition in Malaysia. It helps event organisers manage the event from registration through scoring and results.

## Features

- Set up event years, competitions, categories, venues, and schedules.
- Manage teams, participants, and competition registrations, including team composition rules.
- Configure scoring criteria, points schemes, and tie-break rules.
- Assign judges, record conflicts of interest, and enter and verify scores.
- Calculate results and view winners lists.
- Give event staff and team managers access based on their roles.

## Database status

The app uses ASP.NET Core Razor Pages, Entity Framework Core, and PostgreSQL. Database setup scripts have been excluded from this repository. A database revamp with a simplified schema is pending.

## Contributing and generative AI

- **AI-assisted contributions are welcome.** You are responsible for every change you submit. Review the entire diff by hand, verify generated code and dependencies, and make sure you can explain the change. Mention material AI assistance in the pull request.
- **Verify your work.** Run the relevant tests, lint checks, and build before requesting review. Record what you ran and the results in the pull request. When automated checks are unavailable, describe your manual verification.
- **Use feature branches and pull requests.** Branch from the active development branch and use a descriptive name such as `feature/event-schedule` or `docs/update-readme`. Keep commits focused. Direct pushes and force-pushes to `dev`, `test`, and `main` are prohibited. Open a pull request with the purpose of the change, verification results, and a link to the related issue when available. Merge after review and successful required checks.
- **Use GitHub Issues.** Search existing issues first. For bugs, include steps to reproduce, expected and actual behaviour, the affected version or environment, and sanitised logs or screenshots where helpful. For feature requests, describe the problem and the proposed outcome.
- **Protect sensitive information.** Keep credentials, personal data, and confidential material out of commits, issues, pull requests, and AI prompts. Report suspected security vulnerabilities privately to the maintainer.
