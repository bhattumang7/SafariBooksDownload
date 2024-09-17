# Gets epub fiels for Kindle from SafariBooks.
- Utilizes V2 APIs.
- Search book functionality built right into the application.
- MAUI project that makes this a multi platform application. Works on Android and Windows.
- Downlads content file in parallel. 
- Shows a login page to avoid fuss around cookies that is present in most of the other projects.
- Does not have problem of not downloading images referred in CSS files.
- Gets original table of contents.
- Corrects "display: none" problem when sending to kindle.
- Corrects/adjusts (to their correct relative path in epub) image paths in css.
- Does not have the problem of fonts not downloading.
- Injects "override_v1.css" to every file.
- Download resumes from where it stopped.

Telerik MAUI controls have to be installed for us to be able to run this.
