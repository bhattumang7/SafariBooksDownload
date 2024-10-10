# Gets epub files for Kindle from SafariBooks.
- Utilizes V2 APIs.
- Search book functionality built right into the application.
- MAUI project that makes this a multi-platform application. Works on Android and Windows.
- Download content files in parallel. 
- Shows a login page to avoid the fuss around cookies that are present in most of the other projects.
- Does not have the problem of not downloading images referred to in CSS files.
- Gets original table of contents.
- Corrects the "display: none" problem when sending it to Kindle.
- Corrects/adjusts (to their correct relative path in epub) image paths in CSS.
- Does not have the problem of fonts not downloading.
- Injects "override_v1.css" to every file.
- Download resumes from where it stopped.
- The share button is now built into the application, allowing users to share the file with OneDrive, Kindle, Google Drive, etc. 

### Here are some screen clippings from the Android application: 

Here is how the login page looks like:

<img src="./Login_page.jpg" alt="Login Page" width="200">

Here is what the Search screen looks like: 

<img src="./Search_screen.jpg" alt="Search Screen" width="200">

Here is how the download progress screen looks like: 

<img src="./Dwonload_progress.jpg" alt="Dwonlaod Progress Screen" width="200">

Here is what the success page with the Share to Other Applications and Delete buttons looks like: 

<img src="./Share_success.jpg" alt="Dwonlaod Progress Screen" width="200">



