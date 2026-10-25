using System;
using System.IO;
using UnityEngine;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

public class QuizPackageUploadManager : MonoBehaviour
{
    public const int ChunkSize = 256 * 1024;
    public const long MaxPackageSize = 20L * 1024 * 1024;

    private string expectedVersionID;
    private string expectedClassroomID;
    private string expectedPackageID;

    public bool TryValidatePackage(
        string quizFilePath,
        out string packageFolder)
    {
        packageFolder = null;

        if (string.IsNullOrWhiteSpace(quizFilePath))
        {
            Debug.Log("Quiz upload skipped: No custom Quiz Package selected.");
            return false;
        }

        if (!File.Exists(quizFilePath))
        {
            Debug.LogWarning("Quiz Package file does not exist: " + quizFilePath);
            return false;
        }

        packageFolder = Path.GetDirectoryName(
            Path.GetFullPath(quizFilePath)
        );

        string[] files = Directory.GetFiles(
            packageFolder,
            "*",
            SearchOption.AllDirectories
        );

        long totalSize = 0;

        foreach (string file in files)
        {
            if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;

            FileInfo info = new FileInfo(file);
            totalSize += info.Length;
        }

        if (totalSize > MaxPackageSize)
        {
            Debug.LogWarning(
                "Quiz Package exceeds the 20 MiB upload limit."
            );
            return false;
        }

        Debug.Log(
            $"Quiz Package validated: {Path.GetFileName(packageFolder)} " +
            $"({totalSize / 1024f / 1024f:F2} MiB)"
        );

        return true;
    }

    private string CreatePackageSnapshot(string packageFolder)
    {
        string snapshotRoot = Path.Combine(
            Application.temporaryCachePath,
            "QuizPackageSnapshots"
        );

        string snapshotFolder = Path.Combine(
            snapshotRoot,
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(snapshotFolder);

        try
        {
            string[] files = Directory.GetFiles(
                packageFolder,
                "*",
                SearchOption.AllDirectories
            );

            long totalSize = 0;

            foreach (string sourceFile in files)
            {
                if (sourceFile.EndsWith(
                    ".meta",
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                string relativePath = Path.GetRelativePath(
                    packageFolder,
                    sourceFile
                );

                string destinationFile = Path.Combine(
                    snapshotFolder,
                    relativePath
                );

                string destinationDirectory =
                    Path.GetDirectoryName(destinationFile);

                Directory.CreateDirectory(destinationDirectory);

                File.Copy(
                    sourceFile,
                    destinationFile,
                    false
                );

                totalSize += new FileInfo(destinationFile).Length;

                if (totalSize > MaxPackageSize)
                {
                    throw new IOException(
                        "Quiz Package snapshot exceeds 20 MiB."
                    );
                }
            }

            Debug.Log(
                "Quiz Package snapshot created: " +
                snapshotFolder
            );

            return snapshotFolder;
        }
        catch
        {
            if (Directory.Exists(snapshotFolder))
                Directory.Delete(snapshotFolder, true);

            throw;
        }
    }

    private void DeletePackageSnapshot(string snapshotFolder)
    {
        if (string.IsNullOrEmpty(snapshotFolder))
            return;

        try
        {
            if (Directory.Exists(snapshotFolder))
            {
                Directory.Delete(snapshotFolder, true);

                Debug.Log(
                    "Quiz Package snapshot deleted: " +
                    snapshotFolder
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "Failed to delete Quiz Package snapshot: " + ex
            );
        }
    }
    private bool ArePackageFoldersIdentical(
        string originalFolder,
        string snapshotFolder)
    {
        if (string.IsNullOrEmpty(originalFolder) ||
            string.IsNullOrEmpty(snapshotFolder) ||
            !Directory.Exists(originalFolder) ||
            !Directory.Exists(snapshotFolder))
        {
            return false;
        }

        string[] originalFiles = Directory.GetFiles(
            originalFolder,
            "*",
            SearchOption.AllDirectories
        );

        string[] snapshotFiles = Directory.GetFiles(
            snapshotFolder,
            "*",
            SearchOption.AllDirectories
        );

        Dictionary<string, string> snapshotHashes =
            new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string file in snapshotFiles)
        {
            if (file.EndsWith(
                ".meta",
                StringComparison.OrdinalIgnoreCase))
                continue;

            string relativePath = Path.GetRelativePath(
                snapshotFolder,
                file
            ).Replace('\\', '/');

            snapshotHashes.Add(
                relativePath,
                CalculateSHA256(File.ReadAllBytes(file))
            );
        }

        int originalCount = 0;

        foreach (string file in originalFiles)
        {
            if (file.EndsWith(
                ".meta",
                StringComparison.OrdinalIgnoreCase))
                continue;

            originalCount++;

            string relativePath = Path.GetRelativePath(
                originalFolder,
                file
            ).Replace('\\', '/');

            if (!snapshotHashes.TryGetValue(
                relativePath,
                out string snapshotHash))
            {
                return false;
            }

            string originalHash =
                CalculateSHA256(File.ReadAllBytes(file));

            if (!string.Equals(
                originalHash,
                snapshotHash,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return originalCount == snapshotHashes.Count;
    }

    public bool TryGetQuizCollection(
        out CollectionReference quizCollection)
    {
        quizCollection = null;

        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("Quiz upload: DatabaseManager is missing.");
            return false;
        }

        string classroomID = DatabaseManager.Instance.CurrentDatabase;

        if (string.IsNullOrWhiteSpace(classroomID) ||
            classroomID == "DefaultDatabase")
        {
            Debug.LogWarning(
                "Quiz upload skipped: No active Classroom selected."
            );
            return false;
        }

        if (FirestoreManager.Instance == null ||
            FirestoreManager.Instance.DB == null)
        {
            Debug.LogError("Quiz upload: Firestore is not initialized.");
            return false;
        }

        if (!FirebaseManager.IsAuthenticated)
        {
            Debug.LogWarning(
                "Quiz upload: Firebase authentication is not ready."
            );
            return false;
        }

        quizCollection = FirestoreManager.Instance.QuizCollection();

        Debug.Log(
            "Quiz upload target: Classroom/" +
            classroomID + "/quizzes"
        );

        return true;
    }

    private bool isUploading = false;

    public async Task<bool> UploadSelectedPackageAsync()
    {
        if (isUploading)
        {
            Debug.LogWarning("A Quiz Package upload is already running.");
            return false;
        }

        string quizPath = QuizMakerManager.SelectedQuizFilePath;

        if (!TryValidatePackage(quizPath, out string packageFolder))
            return false;

        if (!TryGetQuizCollection(out CollectionReference collection))
            return false;

        isUploading = true;

        string packageName = Path.GetFileNameWithoutExtension(quizPath);
        string packageID = packageName;
        
        string classroomID =DatabaseManager.Instance.CurrentDatabase;

        // A new version is created for each upload.
        string versionID = Guid.NewGuid().ToString("N");

        DocumentReference packageDoc = collection.Document(packageID);

        DocumentReference versionDoc = packageDoc
            .Collection("versions")
            .Document(versionID);

        string snapshotFolder = null;

        try
        {
            QuizPackageSyncData localSync =
                QuizPackageSyncManager.Load(
                    classroomID,
                    packageID
                );

            if (localSync == null)
            {
                Debug.LogWarning(
                    "Quiz Package upload blocked: " +
                    "No verified synchronization record exists for " +
                    packageID
                );

                return false;
            }

            string expectedVersion = localSync.baseVersionID;

            if (await HasVersionConflictAsync(
                packageDoc,
                classroomID,
                packageID))
            {
                Debug.LogWarning(
                    "Quiz Package upload blocked: " +
                    "A synchronization conflict was detected."
                );

                return false;
            }


            if (await IsPackageUnchangedAsync(
                packageDoc,
                packageFolder))
            {
                // Check again in case another computer published
                // a new version while we compared the files.
                if (await HasVersionConflictAsync(
                    packageDoc,
                    classroomID,
                    packageID))
                {
                    Debug.LogWarning(
                        "Quiz Package changed remotely during verification. " +
                        "Upload cannot be skipped safely."
                    );

                    return false;
                }

                if (DatabaseManager.Instance == null ||
                    DatabaseManager.Instance.CurrentDatabase != classroomID ||
                    QuizMakerManager.SelectedQuizFilePath != quizPath)
                {
                    Debug.LogWarning(
                        "Quiz Package selection or Classroom changed " +
                        "during verification."
                    );

                    return false;
                }

                Debug.Log(
                    "Quiz Package is unchanged. Skipping Firestore upload: " +
                    packageName
                );

                return true;
            }


            Debug.Log("Starting Quiz Package upload: " + packageName);


            snapshotFolder = CreatePackageSnapshot(packageFolder);

            // Ensure the original package did not change
            // while the snapshot was being created.
            if (!ArePackageFoldersIdentical(
                packageFolder,
                snapshotFolder))
            {
                Debug.LogWarning(
                    "Quiz Package changed while creating the snapshot. " +
                    "Upload cancelled."
                );

                return false;
            }


            await versionDoc.SetAsync(new Dictionary<string, object>
        {
            { "status", "uploading" },
            { "createdAt", FieldValue.ServerTimestamp }
        });

            string[] files = Directory.GetFiles(
            snapshotFolder,
            "*",
            SearchOption.AllDirectories
            );

            int uploadedFiles = 0;

            foreach (string filePath in files)
            {
                if (filePath.EndsWith(".meta",
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                await UploadFileAsync(
                packageDoc,
                versionDoc,
                snapshotFolder,
                filePath
                );

                uploadedFiles++;
            }

            // Mark the version complete before publishing it.
            await versionDoc.UpdateAsync(new Dictionary<string, object>
        {
            { "status", "complete" },
            { "fileCount", uploadedFiles },
            { "completedAt", FieldValue.ServerTimestamp }
        });

            // Verify that the original Quiz Package still matches
            // the snapshot after all files have been uploaded.
            if (!ArePackageFoldersIdentical(
                packageFolder,
                snapshotFolder))
            {
                Debug.LogWarning(
                    "Quiz Package changed during upload. " +
                    "Publication cancelled to avoid recording " +
                    "outdated synchronization metadata."
                );

                return false;
            }

            // Confirm that the active Classroom and selected Quiz
            // Package have not changed during the upload.
            if (DatabaseManager.Instance == null ||
                DatabaseManager.Instance.CurrentDatabase != classroomID ||
                QuizMakerManager.SelectedQuizFilePath != quizPath)
            {
                Debug.LogWarning(
                    "Quiz Package publication cancelled: " +
                    "The active Classroom or selected Quiz Package changed."
                );

                return false;
            }

            // Publish only if Firestore still has the version
            // recorded in our local synchronization metadata.
            bool published = await PublishVersionSafelyAsync(
                packageDoc,
                classroomID,
                packageID,
                versionID,
                expectedVersion
            );


            if (!published)
            {
                Debug.LogWarning(
                    "Quiz Package upload completed, but publication " +
                    "was rejected. Local files were preserved."
                );

                return false;
            }

            Debug.Log(
                $"Quiz Package uploaded successfully: {packageName} " +
                $"({uploadedFiles} files)"
            );

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Quiz Package upload failed: " + ex);
            return false;
        }
        finally
        {
            DeletePackageSnapshot(snapshotFolder);
            isUploading = false;
        }
    }

    private async Task UploadFileAsync(
        DocumentReference packageDoc,
        DocumentReference versionDoc,
        string packageFolder,
        string filePath)
    {
        string relativePath = Path.GetRelativePath(
            packageFolder,
            filePath
        ).Replace('\\', '/');

        byte[] fileBytes = File.ReadAllBytes(filePath);

        // Calculate hash of the file contents.
        string hash = CalculateSHA256(fileBytes);

        // Calculate a stable ID based on the relative file path.
        string fileID = CalculateSHA256(
            System.Text.Encoding.UTF8.GetBytes(relativePath)
        );

        // Upload the binary content only if it doesn't already exist.
        await UploadContentIfMissingAsync(
            packageDoc,
            fileBytes,
            hash
        );

        // Create a reference to the content for this version.
        DocumentReference fileDoc = versionDoc
            .Collection("files")
            .Document(fileID);

        await fileDoc.SetAsync(new Dictionary<string, object>
    {
        { "relativePath", relativePath },
        { "size", fileBytes.Length },
        { "sha256", hash },
        { "contentID", hash }
    });

        Debug.Log(
            $"Registered file: {relativePath} (content: {hash})"
        );
    }


    private async Task<bool> TryClaimContentUploadAsync(
        DocumentReference contentDoc,
        string hash,
        int fileSize,
        int chunkCount,
        string uploadID)
    {
        return await FirestoreManager.Instance.DB
            .RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot =
                    await transaction.GetSnapshotAsync(contentDoc);

                if (snapshot.Exists)
                {
                    if (snapshot.TryGetValue(
                        "status",
                        out string status))
                    {
                        if (status == "complete")
                            return false;

                        if (status == "uploading")
                            return false;
                    }

                    // Do not overwrite an existing content record
                    // whose state we cannot safely identify.
                    return false;
                }

                transaction.Set(
                    contentDoc,
                    new Dictionary<string, object>
                    {
                    { "sha256", hash },
                    { "size", fileSize },
                    { "chunkCount", chunkCount },
                    { "status", "uploading" },
                    { "uploadID", uploadID },
                    { "startedAt", FieldValue.ServerTimestamp }
                    }
                );

                return true;
            });
    }

    private async Task<bool> IsContentCompleteAsync(
        DocumentReference contentDoc,
        string expectedHash,
        int expectedSize,
        int expectedChunkCount)
    {
        DocumentSnapshot snapshot = await contentDoc.GetSnapshotAsync();

        if (!snapshot.Exists)
            return false;

        if (!snapshot.TryGetValue("status", out string status) ||
            status != "complete")
            return false;

        if (!snapshot.TryGetValue("sha256", out string storedHash) ||
            !snapshot.TryGetValue("size", out long storedSize) ||
            !snapshot.TryGetValue("chunkCount", out long storedChunkCount))
        {
            throw new IOException(
                "Completed content record has missing metadata: " +
                expectedHash
            );
        }

        if (storedHash != expectedHash ||
            storedSize != expectedSize ||
            storedChunkCount != expectedChunkCount)
        {
            throw new IOException(
                "Completed content record has inconsistent metadata: " +
                expectedHash
            );
        }

        return true;
    }

    private async Task<bool> WaitForContentAsync(
        DocumentReference contentDoc,
        string hash,
        int fileSize,
        int chunkCount)
    {
        const int maxAttempts = 30;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (await IsContentCompleteAsync(
                contentDoc, hash, fileSize, chunkCount))
            {
                return true;
            }

            await Task.Delay(1000);
        }

        return false;
    }




    private async Task UploadContentIfMissingAsync(
        DocumentReference packageDoc,
        byte[] fileBytes,
        string hash)
    {
        DocumentReference contentDoc = packageDoc
            .Collection("content")
            .Document(hash);

        int chunkCount =
            (fileBytes.Length + ChunkSize - 1) / ChunkSize;

        // Reuse already completed content.
        if (await IsContentCompleteAsync(
            contentDoc, hash, fileBytes.Length, chunkCount))
        {
            Debug.Log("Reusing existing content: " + hash);
            return;
        }

        string uploadID = Guid.NewGuid().ToString("N");

        bool claimed = await TryClaimContentUploadAsync(
            contentDoc,
            hash,
            fileBytes.Length,
            chunkCount,
            uploadID
        );

        if (!claimed)
        {
            // Another computer may be uploading this content.
            if (await WaitForContentAsync(
                contentDoc, hash, fileBytes.Length, chunkCount))
            {
                Debug.Log("Content completed by another uploader: " + hash);
                return;
            }

            throw new IOException(
                "Content upload is incomplete or timed out: " + hash
            );
        }

        // This uploader owns the content record.
        for (int i = 0; i < chunkCount; i++)
        {
            int offset = i * ChunkSize;
            int length = Math.Min(
                ChunkSize,
                fileBytes.Length - offset
            );

            byte[] chunk = new byte[length];

            Array.Copy(
                fileBytes,
                offset,
                chunk,
                0,
                length
            );

            DocumentReference chunkDoc = contentDoc
                .Collection("chunks")
                .Document(i.ToString("D6"));

            await chunkDoc.SetAsync(new Dictionary<string, object>
        {
            { "data", chunk }
        });
        }

        // Only the owner may mark the content complete.
        bool completed = await FirestoreManager.Instance.DB
            .RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot =
                    await transaction.GetSnapshotAsync(contentDoc);

                if (!snapshot.Exists ||
                    !snapshot.TryGetValue("uploadID", out string ownerID) ||
                    ownerID != uploadID ||
                    !snapshot.TryGetValue("status", out string status) ||
                    status != "uploading")
                {
                    return false;
                }

                transaction.Update(
                    contentDoc,
                    new Dictionary<string, object>
                    {
                    { "status", "complete" },
                    { "completedAt", FieldValue.ServerTimestamp }
                    }
                );

                return true;
            });

        if (!completed)
        {
            throw new IOException(
                "Content upload ownership changed before completion: " +
                hash
            );
        }

        Debug.Log(
            $"Uploaded new content: {hash} ({chunkCount} chunks)"
        );
    }


    private string CalculateSHA256(byte[] data)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hashBytes = sha.ComputeHash(data);

            return BitConverter.ToString(hashBytes)
                .Replace("-", "")
                .ToLowerInvariant();
        }
    }

    private async Task<bool> IsPackageUnchangedAsync(
        DocumentReference packageDoc,
        string packageFolder)
    {
        DocumentSnapshot packageSnapshot =
            await packageDoc.GetSnapshotAsync();

        if (!packageSnapshot.Exists ||
            !packageSnapshot.TryGetValue(
                "activeVersion",
                out string activeVersion) ||
            string.IsNullOrEmpty(activeVersion))
        {
            return false;
        }

        DocumentReference versionDoc = packageDoc
            .Collection("versions")
            .Document(activeVersion);

        DocumentSnapshot versionSnapshot =
            await versionDoc.GetSnapshotAsync();

        if (!versionSnapshot.Exists ||
            !versionSnapshot.TryGetValue(
                "status",
                out string status) ||
            status != "complete")
        {
            return false;
        }

        QuerySnapshot fileSnapshot = await versionDoc
            .Collection("files")
            .GetSnapshotAsync();

        Dictionary<string, string> remoteFiles =
            new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (DocumentSnapshot document in fileSnapshot.Documents)
        {
            if (!document.TryGetValue(
                    "relativePath", out string relativePath) ||
                !document.TryGetValue(
                    "sha256", out string hash) ||
                !document.TryGetValue(
                    "contentID", out string contentID) ||
                string.IsNullOrEmpty(relativePath) ||
                string.IsNullOrEmpty(hash) ||
                string.IsNullOrEmpty(contentID))
            {
                // Legacy versions don't have contentID.
                return false;
            }

            if (!remoteFiles.TryAdd(relativePath, hash))
                return false;
        }

        string[] localFiles = Directory.GetFiles(
            packageFolder,
            "*",
            SearchOption.AllDirectories
        );

        int localFileCount = 0;

        foreach (string filePath in localFiles)
        {
            if (filePath.EndsWith(
                ".meta",
                StringComparison.OrdinalIgnoreCase))
                continue;

            localFileCount++;

            string relativePath = Path.GetRelativePath(
                packageFolder,
                filePath
            ).Replace('\\', '/');

            if (!remoteFiles.TryGetValue(
                relativePath,
                out string remoteHash))
            {
                return false;
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            string localHash = CalculateSHA256(fileBytes);

            if (!string.Equals(
                localHash,
                remoteHash,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return localFileCount == remoteFiles.Count;
    }

    public async Task<bool> CaptureExpectedVersionAsync()
    {
        if (!TryGetQuizCollection(
            out CollectionReference collection))
            return false;

        string quizPath = QuizMakerManager.SelectedQuizFilePath;

        if (!TryValidatePackage(
            quizPath,
            out string packageFolder))
            return false;

        string packageID =
            Path.GetFileNameWithoutExtension(quizPath);

        string classroomID =
            DatabaseManager.Instance.CurrentDatabase;

        DocumentReference packageDoc =
            collection.Document(packageID);

        try
        {
            DocumentSnapshot snapshot =
                await packageDoc.GetSnapshotAsync();

            string activeVersion = null;

            if (snapshot.Exists)
            {
                snapshot.TryGetValue(
                    "activeVersion",
                    out activeVersion
                );
            }

            expectedVersionID = activeVersion;
            expectedClassroomID = classroomID;
            expectedPackageID = packageID;

            Debug.Log(
                "Captured Quiz Package base version: " +
                (expectedVersionID ?? "(new package)")
            );

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Failed to capture Quiz Package version: " + ex
            );

            return false;
        }
    }

    private async Task<bool> HasVersionConflictAsync(
        DocumentReference packageDoc,
        string classroomID,
        string packageID)
    {
        QuizPackageSyncData localSync =
            QuizPackageSyncManager.Load(
                classroomID,
                packageID
            );

        DocumentSnapshot remoteSnapshot =
            await packageDoc.GetSnapshotAsync();

        string remoteVersion = null;

        if (remoteSnapshot.Exists)
        {
            remoteSnapshot.TryGetValue(
                "activeVersion",
                out remoteVersion
            );
        }

        // No local sync record means we cannot establish
        // which Firestore version the local package came from.
        if (localSync == null)
        {
            Debug.LogWarning(
                "Quiz Package has no synchronization history: " +
                packageID
            );

            return true;
        }

        if (localSync.baseVersionID != remoteVersion)
        {
            Debug.LogWarning(
                "Quiz Package version conflict detected.\n" +
                "Package: " + packageID + "\n" +
                "Local base version: " +
                (localSync.baseVersionID ?? "(none)") + "\n" +
                "Firestore active version: " +
                (remoteVersion ?? "(none)")
            );

            return true;
        }

        Debug.Log(
            "Quiz Package version check passed: " +
            packageID
        );

        return false;
    }

    private async Task<bool> PublishVersionSafelyAsync(
        DocumentReference packageDoc,
        string classroomID,
        string packageID,
        string versionID,
        string expectedVersion)
    {
        QuizPackageSyncData currentSync =
            QuizPackageSyncManager.Load(
                classroomID,
                packageID
            );

        if (currentSync == null ||
            currentSync.baseVersionID != expectedVersion)
        {
            Debug.LogWarning(
                "Quiz Package publication cancelled: " +
                "Local synchronization metadata changed during upload."
            );

            return false;
        }

        try
        {
            bool published = await FirestoreManager.Instance.DB
                .RunTransactionAsync(async transaction =>
                {
                    DocumentSnapshot snapshot =
                        await transaction.GetSnapshotAsync(packageDoc);

                    string currentVersion = null;

                    if (snapshot.Exists)
                    {
                        snapshot.TryGetValue(
                            "activeVersion",
                            out currentVersion
                        );
                    }

                    if (currentVersion != expectedVersion)
                    {
                        return false;
                    }

                    transaction.Set(
                        packageDoc,
                        new Dictionary<string, object>
                        {
                        { "packageName", packageID },
                        { "activeVersion", versionID },
                        { "updatedAt", FieldValue.ServerTimestamp }
                        },
                        SetOptions.MergeAll
                    );

                    return true;
                });

            if (!published)
            {
                Debug.LogWarning(
                    "Quiz Package publication rejected because " +
                    "Firestore has a different active version: " +
                    packageID
                );

                return false;
            }

            QuizPackageSyncManager.Save(
                classroomID,
                packageID,
                versionID
            );

            Debug.Log(
                "Quiz Package version published safely: " +
                packageID + " -> " + versionID
            );

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Transactional Quiz Package publication failed: " + ex
            );

            return false;
        }
    }

    public async Task<bool> InitializeExistingPackageSyncAsync()
    {
        string quizPath = QuizMakerManager.SelectedQuizFilePath;

        if (!TryValidatePackage(
            quizPath,
            out string packageFolder))
            return false;

        if (!TryGetQuizCollection(
            out CollectionReference collection))
            return false;

        string classroomID =
            DatabaseManager.Instance.CurrentDatabase;

        string packageID =
            Path.GetFileNameWithoutExtension(quizPath);

        DocumentReference packageDoc =
            collection.Document(packageID);

        QuizPackageSyncData existingSync =
            QuizPackageSyncManager.Load(
                classroomID,
                packageID
            );

        if (existingSync != null)
        {
            Debug.LogWarning(
                "Quiz Package already has synchronization metadata. " +
                "Initialization skipped: " + packageID
            );

            return false;
        }

        try
        {
            DocumentSnapshot packageSnapshot =
                await packageDoc.GetSnapshotAsync();

            if (!packageSnapshot.Exists ||
                !packageSnapshot.TryGetValue(
                    "activeVersion",
                    out string activeVersion) ||
                string.IsNullOrEmpty(activeVersion))
            {
                Debug.LogWarning(
                    "No published Quiz Package version found: " +
                    packageID
                );

                return false;
            }

            DocumentReference versionDoc = packageDoc
                .Collection("versions")
                .Document(activeVersion);

            DocumentSnapshot versionSnapshot =
                await versionDoc.GetSnapshotAsync();

            if (!versionSnapshot.Exists ||
                !versionSnapshot.TryGetValue(
                    "status",
                    out string status) ||
                status != "complete")
            {
                Debug.LogWarning(
                    "Active Quiz Package version is not complete."
                );

                return false;
            }

            QuerySnapshot manifestSnapshot =
                await versionDoc.Collection("files").GetSnapshotAsync();

            Dictionary<string, string> remoteFiles =
                new Dictionary<string, string>(
                    StringComparer.Ordinal
                );

            foreach (DocumentSnapshot fileSnapshot
                in manifestSnapshot.Documents)
            {
                string relativePath =
                    fileSnapshot.GetValue<string>("relativePath");

                string hash =
                    fileSnapshot.GetValue<string>("sha256");

                if (remoteFiles.ContainsKey(relativePath))
                {
                    Debug.LogWarning(
                        "Duplicate path in Firestore manifest: " +
                        relativePath
                    );

                    return false;
                }

                remoteFiles.Add(relativePath, hash);
            }

            string[] localFiles = Directory.GetFiles(
                packageFolder,
                "*",
                SearchOption.AllDirectories
            );

            int checkedFiles = 0;

            foreach (string localFile in localFiles)
            {
                if (localFile.EndsWith(
                    ".meta",
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                string relativePath = Path.GetRelativePath(
                    packageFolder,
                    localFile
                ).Replace('\\', '/');

                if (!remoteFiles.TryGetValue(
                    relativePath,
                    out string remoteHash))
                {
                    Debug.LogWarning(
                        "Local file missing from Firestore manifest: " +
                        relativePath
                    );

                    return false;
                }

                byte[] fileBytes = File.ReadAllBytes(localFile);

                string localHash;

                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hashBytes =
                        sha.ComputeHash(fileBytes);

                    localHash = BitConverter.ToString(hashBytes)
                        .Replace("-", "")
                        .ToLowerInvariant();
                }

                if (!string.Equals(
                    localHash,
                    remoteHash,
                    StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning(
                        "Quiz Package file differs from Firestore: " +
                        relativePath
                    );

                    return false;
                }

                checkedFiles++;
            }

            if (checkedFiles != remoteFiles.Count)
            {
                Debug.LogWarning(
                    "Quiz Package file count mismatch. " +
                    $"Local: {checkedFiles}, " +
                    $"Firestore: {remoteFiles.Count}"
                );

                return false;
            }

            DocumentSnapshot latestPackageSnapshot = await packageDoc.GetSnapshotAsync();

            if (!latestPackageSnapshot.Exists ||
                !latestPackageSnapshot.TryGetValue(
                    "activeVersion",
                    out string latestActiveVersion) ||
                latestActiveVersion != activeVersion)
            {
                Debug.LogWarning(
                    "Quiz Package synchronization initialization cancelled: " +
                    "Firestore active version changed during verification."
                );

                return false;
            }

            if (DatabaseManager.Instance == null ||
            DatabaseManager.Instance.CurrentDatabase != classroomID ||
            QuizMakerManager.SelectedQuizFilePath != quizPath)
            {
                Debug.LogWarning(
                    "Quiz Package synchronization initialization cancelled: " +
                    "The active Classroom or selected Quiz Package changed."
                );

                return false;
            }

            QuizPackageSyncManager.Save(
                classroomID,
                packageID,
                activeVersion
            );

            Debug.Log(
                "Quiz Package synchronization initialized successfully: " +
                packageID + " (" + checkedFiles + " verified files)"
            );

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Quiz Package synchronization initialization failed: " +
                ex
            );

            return false;
        }
    }

    [ContextMenu("Preview Quiz Package Cleanup")]
    public async void PreviewQuizPackageCleanup()
    {
        if (isUploading)
        {
            Debug.LogWarning(
                "Cleanup preview skipped: Upload in progress."
            );
            return;
        }

        string quizPath = QuizMakerManager.SelectedQuizFilePath;

        if (string.IsNullOrWhiteSpace(quizPath))
        {
            Debug.LogWarning(
                "Cleanup preview skipped: No Quiz Package selected."
            );
            return;
        }

        if (!TryGetQuizCollection(
            out CollectionReference collection))
            return;

        string packageName = Path.GetFileNameWithoutExtension(quizPath);

        DocumentReference packageDoc =
            collection.Document(packageName);

        try
        {
            DocumentSnapshot packageSnapshot =
                await packageDoc.GetSnapshotAsync();

            if (!packageSnapshot.Exists ||
                !packageSnapshot.TryGetValue(
                    "activeVersion",
                    out string activeVersion))
            {
                Debug.LogWarning(
                    "Cleanup preview: No active package version found."
                );
                return;
            }

            DocumentReference activeVersionDoc = packageDoc
                .Collection("versions")
                .Document(activeVersion);

            DocumentSnapshot activeSnapshot =
                await activeVersionDoc.GetSnapshotAsync();

            if (!activeSnapshot.Exists ||
                !activeSnapshot.TryGetValue(
                    "status", out string activeStatus) ||
                activeStatus != "complete")
            {
                Debug.LogWarning(
                    "Cleanup preview: Active version is not complete."
                );
                return;
            }

            // Collect binary content referenced by the active version.
            HashSet<string> activeContentIDs = new HashSet<string>();

            QuerySnapshot activeFiles = await activeVersionDoc
                .Collection("files")
                .GetSnapshotAsync();

            foreach (DocumentSnapshot file in activeFiles.Documents)
            {
                if (file.TryGetValue(
                    "contentID",
                    out string contentID))
                {
                    activeContentIDs.Add(contentID);
                }
            }

            QuerySnapshot versions = await packageDoc
                .Collection("versions")
                .GetSnapshotAsync();

            int obsoleteVersionCount = 0;

            foreach (DocumentSnapshot version in versions.Documents)
            {
                if (version.Id == activeVersion)
                    continue;

                Debug.Log(
                    "[Cleanup Preview] Obsolete version: " +
                    version.Id
                );

                obsoleteVersionCount++;
            }

            QuerySnapshot contents = await packageDoc
                .Collection("content")
                .GetSnapshotAsync();

            int unusedContentCount = 0;
            long unusedBytes = 0;

            foreach (DocumentSnapshot content in contents.Documents)
            {
                if (activeContentIDs.Contains(content.Id))
                    continue;

                long size = 0;

                if (content.TryGetValue("size", out long storedSize))
                    size = storedSize;

                unusedBytes += size;
                unusedContentCount++;

                Debug.Log(
                    "[Cleanup Preview] Unused content: " +
                    content.Id +
                    " (" + size + " bytes)"
                );
            }

            Debug.Log(
                "===== QUIZ PACKAGE CLEANUP PREVIEW =====\n" +
                "Package: " + packageName + "\n" +
                "Active version: " + activeVersion + "\n" +
                "Obsolete versions: " + obsoleteVersionCount + "\n" +
                "Unused content records: " + unusedContentCount + "\n" +
                "Potential unused content size: " +
                (unusedBytes / 1024f / 1024f).ToString("F2") +
                " MiB\n" +
                "No Firestore documents were deleted."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Quiz Package cleanup preview failed: " + ex
            );
        }
    }


    [ContextMenu("Test Quiz Package Upload")]
    public async void TestQuizPackageUpload()
    {
        Debug.Log("Starting manual Quiz Package upload test...");

        bool success = await UploadSelectedPackageAsync();

        if (success)
        {
            Debug.Log("TEST PASSED: Quiz Package uploaded successfully.");
        }

        else
        {
            Debug.LogWarning("TEST FAILED: Quiz Package upload was unsuccessful.");
        }
    }


    [ContextMenu("Clean Up Obsolete Quiz Versions")]
    public async void CleanUpObsoleteQuizVersions()
    {
        if (isUploading)
        {
            Debug.LogWarning(
                "Cleanup skipped: A Quiz Package upload is running."
            );
            return;
        }

        string quizPath = QuizMakerManager.SelectedQuizFilePath;

        if (string.IsNullOrWhiteSpace(quizPath))
        {
            Debug.LogWarning(
                "Cleanup skipped: No Quiz Package selected."
            );
            return;
        }

        if (!TryGetQuizCollection(
            out CollectionReference collection))
            return;

        string packageName =
            Path.GetFileNameWithoutExtension(quizPath);

        DocumentReference packageDoc =
            collection.Document(packageName);

        isUploading = true;

        try
        {
            DocumentSnapshot packageSnapshot =
                await packageDoc.GetSnapshotAsync();

            if (!packageSnapshot.Exists ||
                !packageSnapshot.TryGetValue(
                    "activeVersion",
                    out string activeVersion) ||
                string.IsNullOrWhiteSpace(activeVersion))
            {
                Debug.LogWarning(
                    "Cleanup skipped: No active version found."
                );
                return;
            }

            DocumentReference activeVersionDoc = packageDoc
                .Collection("versions")
                .Document(activeVersion);

            DocumentSnapshot activeSnapshot =
                await activeVersionDoc.GetSnapshotAsync();

            if (!activeSnapshot.Exists ||
                !activeSnapshot.TryGetValue(
                    "status", out string activeStatus) ||
                activeStatus != "complete")
            {
                Debug.LogWarning(
                    "Cleanup skipped: Active version is incomplete."
                );
                return;
            }

            QuerySnapshot versions = await packageDoc
                .Collection("versions")
                .GetSnapshotAsync();

            int deletedVersions = 0;
            int skippedVersions = 0;

            foreach (DocumentSnapshot version in versions.Documents)
            {
                if (version.Id == activeVersion)
                    continue;

                // Never delete an incomplete or currently uploading version.
                if (!version.TryGetValue(
                    "status", out string status) ||
                    status != "complete")
                {
                    Debug.Log(
                        "Skipping incomplete version: " + version.Id
                    );

                    skippedVersions++;
                    continue;
                }

                // Check the currently published version again.
                DocumentSnapshot latestPackage =
                    await packageDoc.GetSnapshotAsync();

                if (!latestPackage.Exists ||
                    !latestPackage.TryGetValue(
                        "activeVersion",
                        out string latestActiveVersion))
                {
                    Debug.LogWarning(
                        "Cleanup stopped: Active version unavailable."
                    );
                    return;
                }

                if (latestActiveVersion != activeVersion)
                {
                    Debug.LogWarning(
                        "Cleanup stopped: Active version changed."
                    );
                    return;
                }

                Debug.Log(
                    "Deleting obsolete version: " + version.Id
                );

                await DeleteVersionAsync(
                    packageDoc.Collection("versions").Document(version.Id)
                );

                deletedVersions++;
            }

            Debug.Log(
                "===== QUIZ PACKAGE CLEANUP COMPLETE ====="
            );

            Debug.Log("Package: " + packageName);
            Debug.Log("Deleted versions: " + deletedVersions);
            Debug.Log("Skipped versions: " + skippedVersions);
            Debug.Log("Preserved active version: " + activeVersion);
            Debug.Log("Shared content was not modified.");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Quiz Package cleanup failed: " + ex
            );
        }
        finally
        {
            isUploading = false;
        }
    }

    private async Task DeleteVersionAsync(
        DocumentReference versionDoc)
    {
        QuerySnapshot files = await versionDoc
            .Collection("files")
            .GetSnapshotAsync();

        foreach (DocumentSnapshot file in files.Documents)
        {
            DocumentReference fileDoc = file.Reference;

            // Legacy uploads stored binary chunks inside each file.
            QuerySnapshot chunks = await fileDoc
                .Collection("chunks")
                .GetSnapshotAsync();

            foreach (DocumentSnapshot chunk in chunks.Documents)
            {
                await chunk.Reference.DeleteAsync();
            }

            // Delete the file manifest after its chunks.
            await fileDoc.DeleteAsync();
        }

        // Delete the version only after its files are removed.
        await versionDoc.DeleteAsync();
    }

    [ContextMenu("Test Quiz Package Sync Initialization")]
    public async void TestQuizPackageSyncInitialization()
    {
        Debug.Log(
            "Starting Quiz Package synchronization initialization test..."
        );

        bool success = await InitializeExistingPackageSyncAsync();

        if (success)
        {
            Debug.Log(
                "TEST PASSED: Quiz Package synchronization initialized."
            );
        }
        else
        {
            Debug.LogWarning(
                "TEST NOT COMPLETED: Quiz Package synchronization " +
                "initialization was rejected or failed."
            );
        }
    }

}
