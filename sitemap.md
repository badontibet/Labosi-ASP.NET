# NAS Indexer Routing Sitemap

This file documents the routing model implemented in `Program.cs` and the current MVC controllers. The application uses custom conventional routes for readable Lab 3 URLs, compatibility conventional routes for the older singular URLs, and one default conventional MVC route.

`app.MapControllers()` is present in `Program.cs`, but the current controllers do not define attribute route templates, so there are no active attribute routes to list.

## Route Count

- Documented configured route entries: 26
- Lab 3 readable custom routes: 13
- Default conventional routes counted as Lab 3 custom routes: 0
- Legacy compatibility routes counted as Lab 3 custom routes: 0

## Default Route

The default conventional route remains available and is not counted as a Lab 3 custom route.

| URL pattern | HTTP method | Controller | Action | View used | Route type | Parameters and constraints | Counts as Lab 3 custom route |
|---|---|---|---|---|---|---|---|
| `/` and `/{controller=Home}/{action=Index}/{id?}` | GET | `HomeController` by default; otherwise controller selected by URL | `Index` by default; otherwise action selected by URL | `Views/{Controller}/{Action}.cshtml` | Default conventional | `controller` optional, `action` optional, `id` optional with no constraint | No |

## Readable Lab 3 Custom Routes

These are the readable routes used by navigation and links in the Razor views.

| URL pattern | HTTP method | Controller | Action | View used | Route type | Parameters and constraints | Counts as Lab 3 custom route |
|---|---|---|---|---|---|---|---|
| `/dashboard` | GET | `HomeController` | `Index` | `Views/Home/Index.cshtml` | Custom conventional | none | Yes |
| `/servers` | GET | `NasServersController` | `Index` | `Views/NasServers/Index.cshtml` | Custom conventional | none | Yes |
| `/servers/{id:int}` | GET | `NasServersController` | `Details` | `Views/NasServers/Details.cshtml` | Custom conventional | `id:int` | Yes |
| `/scan-jobs` | GET | `ScanJobsController` | `Index` | `Views/ScanJobs/Index.cshtml` | Custom conventional | none | Yes |
| `/scan-jobs/{id:int}` | GET | `ScanJobsController` | `Details` | `Views/ScanJobs/Details.cshtml` | Custom conventional | `id:int` | Yes |
| `/directories` | GET | `DirectoriesController` | `Index` | `Views/Directories/Index.cshtml` | Custom conventional | none | Yes |
| `/directories/{id:int}` | GET | `DirectoriesController` | `Details` | `Views/Directories/Details.cshtml` | Custom conventional | `id:int` | Yes |
| `/files` | GET | `FileItemsController` | `Index` | `Views/FileItems/Index.cshtml` | Custom conventional | none | Yes |
| `/files/{id:int}` | GET | `FileItemsController` | `Details` | `Views/FileItems/Details.cshtml` | Custom conventional | `id:int` | Yes |
| `/tags` | GET | `TagsController` | `Index` | `Views/Tags/Index.cshtml` | Custom conventional | none | Yes |
| `/tags/{id:int}` | GET | `TagsController` | `Details` | `Views/Tags/Details.cshtml` | Custom conventional | `id:int` | Yes |
| `/admins` | GET | `AdminsController` | `Index` | `Views/Admins/Index.cshtml` | Custom conventional | none | Yes |
| `/admins/{id:int}` | GET | `AdminsController` | `Details` | `Views/Admins/Details.cshtml` | Custom conventional | `id:int` | Yes |

## Legacy Compatibility Routes

These custom conventional routes preserve older singular URLs. They are available, but they are compatibility routes and are not counted as Lab 3 readable custom routes.

| URL pattern | HTTP method | Controller | Action | View used | Route type | Parameters and constraints | Counts as Lab 3 custom route |
|---|---|---|---|---|---|---|---|
| `/NasServer` | GET | `NasServersController` | `Index` | `Views/NasServers/Index.cshtml` | Custom conventional | none | No |
| `/NasServer/Details/{id:int}` | GET | `NasServersController` | `Details` | `Views/NasServers/Details.cshtml` | Custom conventional | `id:int` | No |
| `/ScanJob` | GET | `ScanJobsController` | `Index` | `Views/ScanJobs/Index.cshtml` | Custom conventional | none | No |
| `/ScanJob/Details/{id:int}` | GET | `ScanJobsController` | `Details` | `Views/ScanJobs/Details.cshtml` | Custom conventional | `id:int` | No |
| `/DirectoryItem` | GET | `DirectoriesController` | `Index` | `Views/Directories/Index.cshtml` | Custom conventional | none | No |
| `/DirectoryItem/Details/{id:int}` | GET | `DirectoriesController` | `Details` | `Views/Directories/Details.cshtml` | Custom conventional | `id:int` | No |
| `/FileItem` | GET | `FileItemsController` | `Index` | `Views/FileItems/Index.cshtml` | Custom conventional | none | No |
| `/FileItem/Details/{id:int}` | GET | `FileItemsController` | `Details` | `Views/FileItems/Details.cshtml` | Custom conventional | `id:int` | No |
| `/FileTag` | GET | `TagsController` | `Index` | `Views/Tags/Index.cshtml` | Custom conventional | none | No |
| `/FileTag/Details/{id:int}` | GET | `TagsController` | `Details` | `Views/Tags/Details.cshtml` | Custom conventional | `id:int` | No |
| `/SystemAdmin` | GET | `AdminsController` | `Index` | `Views/Admins/Index.cshtml` | Custom conventional | none | No |
| `/SystemAdmin/Details/{id:int}` | GET | `AdminsController` | `Details` | `Views/Admins/Details.cshtml` | Custom conventional | `id:int` | No |

## Controller Notes

- `HomeController.Index` returns the dashboard view.
- `NasServersController`, `ScanJobsController`, `DirectoriesController`, `FileItemsController`, `TagsController`, and `AdminsController` each expose `Index` and `Details(int id)` actions.
- Detail routes use the `id:int` constraint in custom conventional routes.
- The current controllers do not contain `[Route]`, `[HttpGet]`, or other attribute route templates.
