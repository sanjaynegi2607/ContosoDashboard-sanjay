# Quickstart: Document Upload and Management

## Goal

This feature adds secure document upload, metadata management, sharing, and project-based access to ContosoDashboard while keeping the app offline and training-friendly.

## Local setup

1. Open the solution in VS Code or Visual Studio.
2. Restore dependencies with `dotnet restore`.
3. Run the app with `dotnet run` from the `ContosoDashboard` folder.
4. Log in as any mock user and navigate to the document or project pages.

## Upload flow

1. Select a supported file.
2. Enter the document title and category.
3. Optionally add a description, tags, and project association.
4. Submit the upload and verify the success message.

## Access flow

1. Open the personal documents page or a project detail page.
2. Filter or search for the document by category, title, or tag.
3. Download or preview the file if the user has permission.

## Sharing flow

1. Open a document detail or action menu.
2. Select a user to share with.
3. Verify the document is available in the recipient’s shared-with-me view and that an in-app notification is raised.
