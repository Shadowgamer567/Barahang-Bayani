using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using System.Security.Cryptography;

public class QuizPackageDownloadManager : MonoBehaviour
{
    private bool isDownloading = false;

    public class QuizPackageFileInfo
    {
        public string relativePath;
        public string contentID;
        public string sha256;
        public long size;
    }

    public class QuizPackageManifest
    {
        public string classroomID;
        public string packageID;
        public string versionID;

        public List<QuizPackageFileInfo> files =
            new List<QuizPackageFileInfo>();
    }

    public bool TryGetQuizCollection(out CollectionReference quizCollection)
    {
        quizCollection = null;

        if (DatabaseManager.Instance == null)
        {
            Debug.LogError(
                "Quiz download: DatabaseManager is missing."
            );
            return false;
        }

        string classroomID =
            DatabaseManager.Instance.CurrentDatabase;

        if (string.IsNullOrWhiteSpace(classroomID) ||
            classroomID == "DefaultDatabase")
        {
            Debug.LogWarning(
                "Quiz download skipped: No active Classroom selected."
            );
            return false;
        }

        if (FirestoreManager.Instance == null ||
            FirestoreManager.Instance.DB == null)
        {
            Debug.LogError(
                "Quiz download: Firestore is not initialized."
            );
            return false;
        }

        if (!FirebaseManager.IsAuthenticated)
        {
            Debug.LogWarning(
                "Quiz download: Firebase authentication is not ready."
            );
            return false;
        }

        quizCollection =
            FirestoreManager.Instance.QuizCollection();

        return true;
    }

    public async Task<List<string>> GetPublishedQuizPackagesAsync()
    {
        List<string> packageIDs = new List<string>();

        if (!TryGetQuizCollection(
            out CollectionReference quizCollection))
        {
            return packageIDs;
        }

        string classroomID =
            DatabaseManager.Instance.CurrentDatabase;

        try
        {
            QuerySnapshot snapshot =
                await quizCollection.GetSnapshotAsync();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                // Ignore packages without a published version.
                if (!document.TryGetValue(
                    "activeVersion",
                    out string activeVersion) ||
                    string.IsNullOrWhiteSpace(activeVersion))
                {
                    continue;
                }

                // Only include packages whose active version
                // has finished uploading.
                DocumentSnapshot versionSnapshot =
                    await document.Reference
                        .Collection("versions")
                        .Document(activeVersion)
                        .GetSnapshotAsync();

                if (!versionSnapshot.Exists ||
                    !versionSnapshot.TryGetValue(
                        "status",
                        out string status) ||
                    status != "complete")
                {
                    continue;
                }

                packageIDs.Add(document.Id);

                Debug.Log(
                    "Published Quiz Package found: " +
                    document.Id +
                    " (Version: " + activeVersion + ")"
                );
            }

            // Avoid returning results for a Classroom
            // that is no longer selected.
            if (DatabaseManager.Instance == null ||
                DatabaseManager.Instance.CurrentDatabase != classroomID)
            {
                Debug.LogWarning(
                    "Quiz Package discovery cancelled: " +
                    "Active Classroom changed."
                );

                packageIDs.Clear();
            }

            Debug.Log(
                "Quiz Package discovery finished. " +
                "Published packages found: " +
                packageIDs.Count
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Failed to discover published Quiz Packages: " + ex
            );

            packageIDs.Clear();
        }

        return packageIDs;
    }


    public async Task<QuizPackageManifest> GetPackageManifestAsync(
        string packageID)
    {
        if (string.IsNullOrWhiteSpace(packageID))
            return null;

        if (!TryGetQuizCollection(
            out CollectionReference quizCollection))
        {
            return null;
        }

        string classroomID =
            DatabaseManager.Instance.CurrentDatabase;

        QuizPackageManifest manifest = new QuizPackageManifest
        {
            classroomID = classroomID,
            packageID = packageID
        };

        try
        {
            DocumentReference packageDoc =
                quizCollection.Document(packageID);

            DocumentSnapshot packageSnapshot =
                await packageDoc.GetSnapshotAsync();

            // Get the currently published version.
            if (!packageSnapshot.Exists ||
                !packageSnapshot.TryGetValue(
                    "activeVersion",
                    out string activeVersion) ||
                string.IsNullOrWhiteSpace(activeVersion))
            {
                Debug.LogWarning(
                    "No published version for package: " + packageID
                );

                return null;
            }

            // Remember exactly which version we're downloading.
            manifest.versionID = activeVersion;

            DocumentReference versionDoc = packageDoc
                .Collection("versions")
                .Document(activeVersion);

            DocumentSnapshot versionSnapshot =
                await versionDoc.GetSnapshotAsync();

            // Only accept completed versions.
            if (!versionSnapshot.Exists ||
                !versionSnapshot.TryGetValue(
                    "status",
                    out string status) ||
                status != "complete")
            {
                Debug.LogWarning(
                    "Quiz Package version is incomplete: " + packageID
                );

                return null;
            }

            // Retrieve the file manifest.
            QuerySnapshot manifestSnapshot =
                await versionDoc
                    .Collection("files")
                    .GetSnapshotAsync();

            if (manifestSnapshot.Count == 0)
            {
                Debug.LogWarning(
                    "Published Quiz Package has no files: " + packageID
                );

                return null;
            }

            foreach (DocumentSnapshot document in manifestSnapshot.Documents)
            {
                QuizPackageFileInfo file = new QuizPackageFileInfo
                {
                    relativePath =
                        document.GetValue<string>("relativePath"),

                    contentID =
                        document.GetValue<string>("contentID"),

                    sha256 =
                        document.GetValue<string>("sha256"),

                    size =
                        document.GetValue<long>("size")
                };

                manifest.files.Add(file);

                Debug.Log(
                    "[Manifest] " + file.relativePath +
                    " (" + file.size + " bytes)"
                );
            }

            // Ensure the published version has not changed
            // while we were retrieving its manifest.
            DocumentSnapshot latestPackageSnapshot =
                await packageDoc.GetSnapshotAsync();

            if (!latestPackageSnapshot.Exists ||
                !latestPackageSnapshot.TryGetValue(
                    "activeVersion",
                    out string latestVersion) ||
                latestVersion != manifest.versionID)
            {
                Debug.LogWarning(
                    "Quiz Package version changed during manifest retrieval: " +
                    packageID
                );

                return null;
            }

            // Check that the active Classroom has not changed.
            if (DatabaseManager.Instance == null ||
                DatabaseManager.Instance.CurrentDatabase != classroomID)
            {
                Debug.LogWarning(
                    "Manifest retrieval cancelled: Classroom changed."
                );

                return null;
            }

            Debug.Log(
                "Quiz Package manifest loaded: " +
                packageID +
                " (Version: " + manifest.versionID +
                ", Files: " + manifest.files.Count + ")"
            );

            return manifest;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Failed to retrieve Quiz Package manifest: " + ex
            );

            return null;
        }
    }

    private async Task<List<QuizPackageFileInfo>> GetVersionFilesAsync(
        DocumentReference packageDoc,
        string versionID)
    {
        if (string.IsNullOrWhiteSpace(versionID))
            return null;

        DocumentReference versionDoc = packageDoc
            .Collection("versions")
            .Document(versionID);

        DocumentSnapshot versionSnapshot =
            await versionDoc.GetSnapshotAsync();

        if (!versionSnapshot.Exists ||
            !versionSnapshot.TryGetValue(
                "status",
                out string status) ||
            status != "complete")
        {
            return null;
        }

        QuerySnapshot snapshot =
            await versionDoc.Collection("files").GetSnapshotAsync();

        if (snapshot.Count == 0)
            return null;

        List<QuizPackageFileInfo> files =
            new List<QuizPackageFileInfo>();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            files.Add(new QuizPackageFileInfo
            {
                relativePath =
                    document.GetValue<string>("relativePath"),

                contentID =
                    document.GetValue<string>("contentID"),

                sha256 =
                    document.GetValue<string>("sha256"),

                size =
                    document.GetValue<long>("size")
            });
        }

        return files;
    }

    private async Task<bool> HasUnsynchronizedLocalChangesAsync(
        QuizPackageManifest manifest,
        DocumentReference packageDoc)
    {
        if (manifest == null)
            return true;

        string packageFolder = GetPackageFolder(
            manifest.classroomID,
            manifest.packageID
        );

        if (!Directory.Exists(packageFolder))
            return true;

        QuizPackageSyncData localSync =
            QuizPackageSyncManager.Load(
                manifest.classroomID,
                manifest.packageID
            );

        if (localSync == null ||
            string.IsNullOrWhiteSpace(localSync.baseVersionID))
        {
            Debug.LogWarning(
                "No synchronization baseline for local Quiz Package: " +
                manifest.packageID
            );

            return true;
        }

        List<QuizPackageFileInfo> baselineFiles =
            await GetVersionFilesAsync(
                packageDoc,
                localSync.baseVersionID
            );

        if (baselineFiles == null)
        {
            Debug.LogWarning(
                "Cannot verify local Quiz Package baseline: " +
                manifest.packageID
            );

            return true;
        }

        bool matches = AreLocalFilesMatchingManifest(
            packageFolder,
            baselineFiles
        );

        if (!matches)
        {
            Debug.LogWarning(
                "Unsynchronized local Quiz Package changes detected: " +
                manifest.packageID
            );
        }

        return !matches;
    }

    private async Task<byte[]> DownloadFileAsync(
        DocumentReference packageDoc,
        QuizPackageFileInfo file)
    {
        if (file == null ||
            string.IsNullOrWhiteSpace(file.contentID) ||
            string.IsNullOrWhiteSpace(file.sha256) ||
            file.size < 0 ||
            file.size > QuizPackageUploadManager.MaxPackageSize)
        {
            throw new IOException(
                "Invalid Quiz Package file metadata."
            );
        }

        DocumentReference contentDoc = packageDoc
            .Collection("content")
            .Document(file.contentID);

        DocumentSnapshot contentSnapshot =
            await contentDoc.GetSnapshotAsync();

        if (!contentSnapshot.Exists ||
            !contentSnapshot.TryGetValue(
                "status", out string status) ||
            status != "complete")
        {
            throw new IOException(
                "Quiz Package content is missing or incomplete: " +
                file.relativePath
            );
        }

        string storedHash =
            contentSnapshot.GetValue<string>("sha256");

        long storedSize =
            contentSnapshot.GetValue<long>("size");

        long chunkCount =
            contentSnapshot.GetValue<long>("chunkCount");

        if (storedHash != file.sha256 ||
            file.contentID != file.sha256 ||
            storedSize != file.size ||
            chunkCount < 0 ||
            chunkCount !=
                (file.size + QuizPackageUploadManager.ChunkSize - 1)
                / QuizPackageUploadManager.ChunkSize)
        {
            throw new IOException(
                "Quiz Package content metadata mismatch: " +
                file.relativePath
            );
        }

        using (MemoryStream stream = new MemoryStream())
        {
            for (int i = 0; i < chunkCount; i++)
            {
                DocumentReference chunkDoc = contentDoc
                    .Collection("chunks")
                    .Document(i.ToString("D6"));

                DocumentSnapshot chunkSnapshot =
                    await chunkDoc.GetSnapshotAsync();

                if (!chunkSnapshot.Exists ||
                    !chunkSnapshot.TryGetValue(
                        "data", out byte[] chunk))
                {
                    throw new IOException(
                        "Missing Quiz Package chunk: " +
                        file.relativePath + " #" + i
                    );
                }

                int expectedLength = (int)Math.Min(
                    QuizPackageUploadManager.ChunkSize,
                    file.size - stream.Length
                );

                if (chunk.Length != expectedLength)
                {
                    throw new IOException(
                        "Invalid chunk length: " +
                        file.relativePath + " #" + i
                    );
                }

                stream.Write(chunk, 0, chunk.Length);
            }

            byte[] fileBytes = stream.ToArray();

            if (fileBytes.LongLength != file.size)
            {
                throw new IOException(
                    "Downloaded file size mismatch: " +
                    file.relativePath
                );
            }

            using (SHA256 sha = SHA256.Create())
            {
                string downloadedHash = BitConverter.ToString(
                    sha.ComputeHash(fileBytes)
                ).Replace("-", "").ToLowerInvariant();

                if (!string.Equals(
                    downloadedHash,
                    file.sha256,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException(
                        "Downloaded file SHA-256 mismatch: " +
                        file.relativePath
                    );
                }
            }

            Debug.Log(
                "[Download] Verified: " + file.relativePath +
                " (" + fileBytes.Length + " bytes)"
            );

            return fileBytes;
        }
    }

    private async Task<string> DownloadPackageToTemporaryFolderAsync(
        string packageID,
        DocumentReference packageDoc,
        List<QuizPackageFileInfo> files)
    {
        string temporaryFolder = Path.Combine(
            Application.temporaryCachePath,
            "QuizPackageDownloads",
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(temporaryFolder);

        try
        {
            long totalSize = 0;
            HashSet<string> downloadedPaths =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string rootPath = Path.GetFullPath(temporaryFolder);

            foreach (QuizPackageFileInfo file in files)
            {
                if (file == null ||
                    string.IsNullOrWhiteSpace(file.relativePath) ||
                    Path.IsPathRooted(file.relativePath) ||
                    file.relativePath.Contains(":") ||
                    file.relativePath.Contains("\\"))
                {
                    throw new IOException(
                        "Invalid Quiz Package file path."
                    );
                }

                string[] segments = file.relativePath.Split('/');

                foreach (string segment in segments)
                {
                    if (string.IsNullOrWhiteSpace(segment) ||
                        segment == "." ||
                        segment == ".." ||
                        segment.IndexOfAny(
                            Path.GetInvalidFileNameChars()) >= 0)
                    {
                        throw new IOException(
                            "Unsafe Quiz Package file path: " +
                            file.relativePath
                        );
                    }
                }

                string destination = Path.GetFullPath(
                    Path.Combine(
                        temporaryFolder,
                        file.relativePath.Replace(
                            '/',
                            Path.DirectorySeparatorChar)
                    )
                );

                string rootPrefix =
                    rootPath.TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                if (!destination.StartsWith(
                    rootPrefix,
                    StringComparison.OrdinalIgnoreCase) ||
                    !downloadedPaths.Add(destination))
                {
                    throw new IOException(
                        "Invalid or duplicate download destination: " +
                        file.relativePath
                    );
                }

                byte[] fileBytes =
                    await DownloadFileAsync(packageDoc, file);

                totalSize += fileBytes.LongLength;

                if (totalSize > QuizPackageUploadManager.MaxPackageSize)
                {
                    throw new IOException(
                        "Downloaded Quiz Package exceeds 20 MiB."
                    );
                }

                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination)
                );

                File.WriteAllBytes(destination, fileBytes);

                Debug.Log(
                    "[Package Download] Saved temporary file: " +
                    file.relativePath
                );
            }

            Debug.Log(
                "Quiz Package downloaded and verified: " +
                packageID + " (" + files.Count + " files)"
            );

            return temporaryFolder;
        }
        catch
        {
            if (Directory.Exists(temporaryFolder))
            {
                Directory.Delete(temporaryFolder, true);
            }

            throw;
        }
    }

    private string GetQuizPackageRoot()
    {
        return QuizMakerManager.QuizPackagesRoot;
    }

    private string GetClassroomFolder(string classroomID)
    {
        return Path.Combine(
            GetQuizPackageRoot(),
            classroomID
        );
    }

    private string GetPackageFolder(
        string classroomID,
        string packageID)
    {
        return Path.Combine(
            GetClassroomFolder(classroomID),
            packageID
        );
    }

    private bool AreLocalFilesMatchingManifest(
        string packageFolder,
        List<QuizPackageFileInfo> files)
    {
        if (!Directory.Exists(packageFolder) ||
            files == null ||
            files.Count == 0)
        {
            return false;
        }

        try
        {
            HashSet<string> expectedPaths =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            string rootPath = Path.GetFullPath(packageFolder);

            foreach (QuizPackageFileInfo file in files)
            {
                if (file == null ||
                    string.IsNullOrWhiteSpace(file.relativePath) ||
                    string.IsNullOrWhiteSpace(file.sha256) ||
                    file.size < 0)
                {
                    return false;
                }

                string relativePath = file.relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar
                );

                string fullPath = Path.GetFullPath(
                    Path.Combine(packageFolder, relativePath)
                );

                string rootPrefix =
                    rootPath.TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                if (!fullPath.StartsWith(
                    rootPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!File.Exists(fullPath))
                    return false;

                if (!expectedPaths.Add(fullPath))
                    return false;

                FileInfo fileInfo = new FileInfo(fullPath);

                if (fileInfo.Length != file.size)
                    return false;

                using (SHA256 sha = SHA256.Create())
                using (FileStream stream = File.OpenRead(fullPath))
                {
                    string localHash = BitConverter.ToString(
                        sha.ComputeHash(stream)
                    ).Replace("-", "").ToLowerInvariant();

                    if (!string.Equals(
                        localHash,
                        file.sha256,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            // Detect additional local files not present
            // in the synchronized Firestore manifest.
            foreach (string localFile in Directory.GetFiles(
                packageFolder,
                "*",
                SearchOption.AllDirectories))
            {
                if (localFile.EndsWith(
                    ".meta",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!expectedPaths.Contains(
                    Path.GetFullPath(localFile)))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "Failed to compare local Quiz Package files: " +
                ex.Message
            );

            return false;
        }
    }

    private bool InstallDownloadedPackage(
        QuizPackageManifest manifest,
        string temporaryFolder)
    {
        if (manifest == null ||
            string.IsNullOrWhiteSpace(temporaryFolder) ||
            !Directory.Exists(temporaryFolder))
        {
            Debug.LogError("Invalid Quiz Package installation data.");
            return false;
        }

        foreach (string name in new[]
        {
        manifest.classroomID,
        manifest.packageID
    })
        {
            if (string.IsNullOrWhiteSpace(name) ||
                name == "." ||
                name == ".." ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                name.Contains("/") ||
                name.Contains("\\") ||
                name.Contains(":") ||
                Path.IsPathRooted(name))
            {
                Debug.LogError("Unsafe Quiz Package folder name.");
                return false;
            }
        }

        string classroomFolder =
            GetClassroomFolder(manifest.classroomID);

        string destinationFolder =
            GetPackageFolder(
                manifest.classroomID,
                manifest.packageID
            );

        if (Directory.Exists(destinationFolder) ||
            File.Exists(destinationFolder))
        {
            Debug.LogWarning(
                "Quiz Package already exists locally. " +
                "Installation skipped: " + destinationFolder
            );
            return false;
        }

        // Stage on the same drive as the destination.
        string stagingFolder = Path.Combine(
            classroomFolder,
            ".install-" + Guid.NewGuid().ToString("N")
        );

        try
        {
            Directory.CreateDirectory(classroomFolder);
            Directory.CreateDirectory(stagingFolder);

            // Copy all verified files into the staging directory.
            foreach (string sourceFile in Directory.GetFiles(
                temporaryFolder,
                "*",
                SearchOption.AllDirectories))
            {
                string relativePath = sourceFile.Substring(
                    temporaryFolder.TrimEnd(
                        Path.DirectorySeparatorChar).Length + 1
                );

                string stagingFile = Path.Combine(
                    stagingFolder,
                    relativePath
                );

                Directory.CreateDirectory(
                    Path.GetDirectoryName(stagingFile)
                );

                File.Copy(sourceFile, stagingFile);
            }

            // Ensure the destination hasn't appeared meanwhile.
            if (Directory.Exists(destinationFolder) ||
                File.Exists(destinationFolder))
            {
                Debug.LogWarning(
                    "Installation cancelled: Destination already exists."
                );
                return false;
            }

            // Both folders are now on the same drive.
            Directory.Move(stagingFolder, destinationFolder);

            Debug.Log(
                "Quiz Package installed successfully: " +
                destinationFolder
            );

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Quiz Package installation failed: " + ex
            );

            return false;
        }
        finally
        {
            // Remove incomplete staging data if installation failed.
            if (Directory.Exists(stagingFolder))
            {
                try
                {
                    Directory.Delete(stagingFolder, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        "Could not clean installation staging folder: " +
                        ex.Message
                    );
                }
            }
        }
    }

    private bool ReplaceInstalledPackage(
    QuizPackageManifest manifest,
    string temporaryFolder,
    out string backupFolder)
    {
        backupFolder = null;

        if (manifest == null ||
            string.IsNullOrWhiteSpace(temporaryFolder) ||
            !Directory.Exists(temporaryFolder))
        {
            return false;
        }

        string classroomFolder =
            GetClassroomFolder(manifest.classroomID);

        string destinationFolder =
            GetPackageFolder(
                manifest.classroomID,
                manifest.packageID
            );

        if (!Directory.Exists(destinationFolder))
        {
            Debug.LogWarning(
                "Cannot update Quiz Package: Local installation missing."
            );
            return false;
        }

        string operationID = Guid.NewGuid().ToString("N");

        string stagingFolder = Path.Combine(
            classroomFolder,
            ".update-" + operationID
        );

        backupFolder = Path.Combine(
        classroomFolder,
        ".backup-" + operationID
);

        bool backupCreated = false;

        try
        {
            // Stage the verified download on the destination drive.
            Directory.CreateDirectory(stagingFolder);

            foreach (string sourceFile in Directory.GetFiles(
                temporaryFolder,
                "*",
                SearchOption.AllDirectories))
            {
                string relativePath = sourceFile.Substring(
                    temporaryFolder.TrimEnd(
                        Path.DirectorySeparatorChar).Length + 1
                );

                string stagingFile = Path.Combine(
                    stagingFolder,
                    relativePath
                );

                Directory.CreateDirectory(
                    Path.GetDirectoryName(stagingFile)
                );

                File.Copy(sourceFile, stagingFile);
            }

            // Preserve the previous installed version.
            Directory.Move(destinationFolder, backupFolder);
            backupCreated = true;

            // Install the new version.
            Directory.Move(stagingFolder, destinationFolder);

            Debug.Log(
                "Quiz Package replacement completed: " +
                destinationFolder
            );

            // Keep the backup until the caller confirms that
            // synchronization metadata has been saved.
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Quiz Package replacement failed: " + ex
            );

            // Restore the previous version if replacement failed.
            if (backupCreated)
            {
                RestorePackageBackup(
                    destinationFolder,
                    backupFolder
                );
            }

            return false;
        }
        finally
        {
            if (Directory.Exists(stagingFolder))
            {
                try
                {
                    Directory.Delete(stagingFolder, true);
                }
                catch (Exception cleanupEx)
                {
                    Debug.LogWarning(
                        "Could not clean update staging folder: " +
                        cleanupEx.Message
                    );
                }
            }
        }
    }

    private bool RestorePackageBackup(
        string destinationFolder,
        string backupFolder)
    {
        if (string.IsNullOrWhiteSpace(backupFolder) ||
            !Directory.Exists(backupFolder))
        {
            Debug.LogError(
                "Cannot restore Quiz Package: Backup is missing."
            );
            return false;
        }

        try
        {
            // Preserve the failed installation rather than
            // deleting it immediately.
            if (Directory.Exists(destinationFolder))
            {
                string failedFolder =
                    destinationFolder +
                    ".failed-" +
                    Guid.NewGuid().ToString("N");

                Directory.Move(destinationFolder, failedFolder);
            }

            Directory.Move(backupFolder, destinationFolder);

            Debug.Log(
                "Previous Quiz Package restored successfully: " +
                destinationFolder
            );

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Failed to restore Quiz Package backup: " + ex
            );

            return false;
        }
    }

    private void DeletePackageBackup(string backupFolder)
    {
        if (string.IsNullOrWhiteSpace(backupFolder) ||
            !Directory.Exists(backupFolder))
        {
            return;
        }

        try
        {
            Directory.Delete(backupFolder, true);

            Debug.Log(
                "Quiz Package update backup removed: " +
                backupFolder
            );
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "Could not remove Quiz Package backup: " +
                ex.Message
            );
        }
    }

    public async Task<bool> UpdateInstalledPackageAsync(string packageID)
    {
        string installedFolder = null;

        if (isDownloading)
        {
            Debug.LogWarning("Another Quiz Package download is running.");
            return false;
        }

        if (!TryGetQuizCollection(
            out CollectionReference collection))
            return false;

        isDownloading = true;

        string temporaryFolder = null;
        string backupFolder = null;
        bool replacementCompleted = false;

        try
        {
            QuizPackageManifest manifest =
                await GetPackageManifestAsync(packageID);

            if (manifest == null || manifest.files.Count == 0)
                return false;

            string classroomID = manifest.classroomID;

            string destinationFolder =
                GetPackageFolder(classroomID, packageID);

            installedFolder = destinationFolder;

            if (!Directory.Exists(destinationFolder))
            {
                Debug.LogWarning(
                    "Package is not installed. Use first-time installation."
                );
                return false;
            }

            DocumentReference packageDoc =
                collection.Document(packageID);

            QuizPackageSyncData localSync =
                QuizPackageSyncManager.Load(classroomID, packageID);

            if (localSync == null ||
                string.IsNullOrWhiteSpace(localSync.baseVersionID))
            {
                Debug.LogWarning(
                    "Cannot update package without a synchronization baseline."
                );
                return false;
            }

            string expectedBaseVersion = localSync.baseVersionID;

            // Verify local files against the previously synchronized version.
            if (await HasUnsynchronizedLocalChangesAsync(
                manifest, packageDoc))
            {
                Debug.LogWarning(
                    "Update cancelled: Unsynchronized local changes."
                );
                return false;
            }

            if (expectedBaseVersion == manifest.versionID)
            {
                Debug.Log(
                    "Quiz Package already up to date: " + packageID
                );
                return true;
            }

            // Download the new version into a temporary folder.
            temporaryFolder =
                await DownloadPackageToTemporaryFolderAsync(
                    packageID,
                    packageDoc,
                    manifest.files
                );

            // Ensure the Classroom hasn't changed.
            if (DatabaseManager.Instance == null ||
                DatabaseManager.Instance.CurrentDatabase != classroomID)
            {
                Debug.LogWarning(
                    "Update cancelled: Active Classroom changed."
                );
                return false;
            }

            // Ensure Firestore is still publishing the downloaded version.
            DocumentSnapshot latestSnapshot =
                await packageDoc.GetSnapshotAsync();

            if (!latestSnapshot.Exists ||
                !latestSnapshot.TryGetValue(
                    "activeVersion",
                    out string latestVersion) ||
                latestVersion != manifest.versionID)
            {
                Debug.LogWarning(
                    "Update cancelled: Published version changed."
                );
                return false;
            }

            // Ensure synchronization metadata hasn't changed.
            QuizPackageSyncData latestSync =
                QuizPackageSyncManager.Load(classroomID, packageID);

            if (latestSync == null ||
                latestSync.baseVersionID != expectedBaseVersion)
            {
                Debug.LogWarning(
                    "Update cancelled: Local synchronization baseline changed."
                );
                return false;
            }

            // Check local files again immediately before replacement.
            if (await HasUnsynchronizedLocalChangesAsync(
                manifest, packageDoc))
            {
                Debug.LogWarning(
                    "Update cancelled: Local files changed during download."
                );
                return false;
            }

            // Replace the installation, preserving the old version.
            if (!ReplaceInstalledPackage(
                manifest,
                temporaryFolder,
                out backupFolder))
            {
                return false;
            }

            replacementCompleted = true;

            // Record the newly installed version.
            QuizPackageSyncManager.Save(
                classroomID,
                packageID,
                manifest.versionID
            );

            // Confirm that synchronization metadata was saved.
            QuizPackageSyncData confirmedSync =
                QuizPackageSyncManager.Load(classroomID, packageID);

            if (confirmedSync == null ||
                confirmedSync.baseVersionID != manifest.versionID)
            {
                throw new IOException(
                    "Failed to confirm Quiz Package synchronization metadata."
                );
            }

            DeletePackageBackup(backupFolder);
            backupFolder = null;

            Debug.Log(
                "Quiz Package updated successfully: " +
                packageID + " -> " + manifest.versionID
            );

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Quiz Package update failed: " + ex
            );

            if (replacementCompleted && !string.IsNullOrWhiteSpace(backupFolder))
            {
                bool restored = RestorePackageBackup(
                    installedFolder,
                    backupFolder
                );

                if (restored)
                {
                    Debug.LogWarning(
                        "Previous Quiz Package restored. " +
                        "Synchronization metadata should be verified " +
                        "before attempting another update."
                    );
                }
            }

            return false;
        }

        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryFolder) &&
                Directory.Exists(temporaryFolder))
            {
                try
                {
                    Directory.Delete(temporaryFolder, true);
                }
                catch (Exception cleanupEx)
                {
                    Debug.LogWarning(
                        "Could not clean temporary download: " +
                        cleanupEx.Message
                    );
                }
            }

            isDownloading = false;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ContextMenu("Test Quiz Package Discovery")]
    public async void TestQuizPackageDiscovery()
    {
        Debug.Log("Starting Quiz Package discovery test...");

        List<string> packages =
            await GetPublishedQuizPackagesAsync();

        foreach (string packageID in packages)
        {
            Debug.Log(
                "[Discovery Test] Available Quiz Package: " +
                packageID
            );
        }

        Debug.Log(
            "[Discovery Test] Total published packages: " +
            packages.Count
        );
    }

    [ContextMenu("Test Quiz Package Manifest")]
    public async void TestQuizPackageManifest()
    {
        Debug.Log("Starting Quiz Package manifest test...");

        QuizPackageManifest manifest =
            await GetPackageManifestAsync("Test Package");

        if (manifest == null)
        {
            Debug.LogWarning(
                "[Manifest Test] No valid manifest found."
            );
            return;
        }

        Debug.Log(
            "[Manifest Test] Version: " + manifest.versionID
        );

        Debug.Log(
            "[Manifest Test] Total files: " +
            manifest.files.Count
        );
    }


    [ContextMenu("Test Quiz Package File Download")]
    public async void TestQuizPackageFileDownload()
    {
        if (!TryGetQuizCollection(
            out CollectionReference collection))
            return;

        string classroomID =
            DatabaseManager.Instance.CurrentDatabase;

        try
        {
            QuizPackageManifest manifest = await GetPackageManifestAsync("Test Package");

            if (manifest == null || manifest.files.Count == 0)
            {
                Debug.LogWarning("Download test: No package files found.");
                return;
            }

            List<QuizPackageFileInfo> files = manifest.files;

            DocumentReference packageDoc =
                collection.Document("Test Package");

            // Test the first file without saving it locally.
            byte[] downloadedBytes =
                await DownloadFileAsync(packageDoc, files[0]);

            if (DatabaseManager.Instance == null ||
                DatabaseManager.Instance.CurrentDatabase != classroomID)
            {
                Debug.LogWarning(
                    "Download test cancelled: Classroom changed."
                );
                return;
            }

            Debug.Log(
                "[Download Test] PASSED: " +
                files[0].relativePath +
                " (" + downloadedBytes.Length + " verified bytes)"
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Download Test] FAILED: " + ex
            );
        }
    }

    [ContextMenu("Test Full Quiz Package Download")]
    public async void TestFullQuizPackageDownload()
    {
        if (isDownloading)
        {
            Debug.LogWarning(
                "A Quiz Package download is already running."
            );
            return;
        }

        if (!TryGetQuizCollection(
            out CollectionReference collection))
            return;

        isDownloading = true;

        string temporaryFolder = null;

        try
        {
            string packageID = "Test Package";

            QuizPackageManifest manifest = await GetPackageManifestAsync(packageID);

            if (manifest == null || manifest.files.Count == 0)
            {
                Debug.LogWarning(
                    "No valid manifest found for Quiz Package download."
                );
                return;
            }

            List<QuizPackageFileInfo> files = manifest.files;

            DocumentReference packageDoc =
                collection.Document(packageID);

            temporaryFolder =
                await DownloadPackageToTemporaryFolderAsync(
                    packageID,
                    packageDoc,
                    files
                );

            Debug.Log(
                "[Full Download Test] PASSED: " +
                files.Count + " files downloaded and verified."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Full Download Test] FAILED: " + ex
            );
        }
        finally
        {
            if (!string.IsNullOrEmpty(temporaryFolder) &&
                Directory.Exists(temporaryFolder))
            {
                Directory.Delete(temporaryFolder, true);
            }

            isDownloading = false;
        }
    }

    [ContextMenu("Test Quiz Package Installation")]
    public async void TestQuizPackageInstallation()
    {
        if (isDownloading)
            return;

        if (!TryGetQuizCollection(
            out CollectionReference collection))
            return;

        isDownloading = true;
        string temporaryFolder = null;

        try
        {
            string packageID = "Test Package";

            QuizPackageManifest manifest =
                await GetPackageManifestAsync(packageID);

            if (manifest == null || manifest.files.Count == 0)
                return;

            string destinationFolder =
                GetPackageFolder(
                    manifest.classroomID,
                    manifest.packageID
                );

            if (Directory.Exists(destinationFolder))
            {
                Debug.LogWarning(
                    "Installation test skipped: Package already installed."
                );
                return;
            }

            DocumentReference packageDoc =
                collection.Document(packageID);

            temporaryFolder =
                await DownloadPackageToTemporaryFolderAsync(
                    packageID,
                    packageDoc,
                    manifest.files
                );

            // Ensure the Classroom is still active.
            if (DatabaseManager.Instance == null ||
                DatabaseManager.Instance.CurrentDatabase !=
                    manifest.classroomID)
            {
                Debug.LogWarning(
                    "Installation cancelled: Classroom changed."
                );
                return;
            }

            // Ensure the published version is still current.
            DocumentSnapshot latestSnapshot =
                await packageDoc.GetSnapshotAsync();

            if (!latestSnapshot.Exists ||
                !latestSnapshot.TryGetValue(
                    "activeVersion",
                    out string latestVersion) ||
                latestVersion != manifest.versionID)
            {
                Debug.LogWarning(
                    "Installation cancelled: Published version changed."
                );
                return;
            }

            if (!InstallDownloadedPackage(
                manifest,
                temporaryFolder))
            {
                return;
            }

            temporaryFolder = null;

            QuizPackageSyncManager.Save(
                manifest.classroomID,
                manifest.packageID,
                manifest.versionID
            );

            Debug.Log(
                "[Installation Test] PASSED: " +
                manifest.packageID +
                " installed for Classroom " +
                manifest.classroomID
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Installation Test] FAILED: " + ex
            );
        }
        finally
        {
            if (!string.IsNullOrEmpty(temporaryFolder) &&
                Directory.Exists(temporaryFolder))
            {
                Directory.Delete(temporaryFolder, true);
            }

            isDownloading = false;
        }
    }

    [ContextMenu("Test Quiz Package Local Changes")]
    public async void TestQuizPackageLocalChanges()
    {
        if (!TryGetQuizCollection(
            out CollectionReference collection))
            return;

        const string packageID = "Test Package";

        QuizPackageManifest manifest =
            await GetPackageManifestAsync(packageID);

        if (manifest == null)
            return;

        try
        {
            DocumentReference packageDoc =
                collection.Document(packageID);

            bool hasChanges =
                await HasUnsynchronizedLocalChangesAsync(
                    manifest,
                    packageDoc
                );

            Debug.Log(
                "[Local Changes Test] " +
                (hasChanges
                    ? "Local changes detected or baseline unavailable."
                    : "Local package matches synchronized baseline.")
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Local Changes Test] FAILED: " + ex
            );
        }
    }

    [ContextMenu("Test Quiz Package Update")]
    public async void TestQuizPackageUpdate()
    {
        const string packageID = "Test Package";

        Debug.Log(
            "[Update Test] Checking installed Quiz Package: " +
            packageID
        );

        bool result =
            await UpdateInstalledPackageAsync(packageID);

        Debug.Log(
            result
                ? "[Update Test] PASSED."
                : "[Update Test] Update skipped or failed. Check warnings."
        );
    }

}
