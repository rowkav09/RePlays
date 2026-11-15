using RePlays.Utils;
using System;
using System.Collections.Generic;

namespace RePlays.Services {
    internal static class BookmarkService {
        static List<Bookmark> bookmarks = new();
        static int latestBookmarkKeyPress;
        // bookmarks come in from the hotkey and from game integration threads while a recording
        // is being saved; the list is only touched under this lock, saves run one at a time
        static readonly object bookmarkLock = new();
        static readonly object saveLock = new();

        public static void AddBookmark(Bookmark bookmark, DateTime? dateTime = null) {
            if (dateTime == null) {
                dateTime = DateTime.Now;
            }
            int secondsSinceEpoch = (int)(dateTime.Value - new DateTime(1970, 1, 1)).TotalSeconds;

            if ((secondsSinceEpoch - latestBookmarkKeyPress >= 2) || !bookmark.type.Equals(Bookmark.BookmarkType.Manual)) {
                latestBookmarkKeyPress = secondsSinceEpoch;
                double bookmarkTimestamp = RecordingService.GetTotalRecordingTimeInSecondsWithDecimals(dateTime);
                Logger.WriteLine("Adding bookmark: " + bookmarkTimestamp);
                bookmark.time = bookmarkTimestamp;
                lock (bookmarkLock) bookmarks.Add(bookmark);

                if (bookmark.type.Equals(Bookmark.BookmarkType.Manual)) {
                    Functions.PlaySound(Functions.GetResourcesFolder() + "bookmark.wav");
                    RecordingService.ActiveRecorder.TrySaveReplayBufferAndBookmarks();
                }

            }
        }

        public static void SaveBookmarks(string videoPath) {
            lock (saveLock) {
                List<Bookmark> pending;
                lock (bookmarkLock) {
                    if (bookmarks.Count == 0) return;
                    pending = new List<Bookmark>(bookmarks);
                }
                Logger.WriteLine($"Saving {pending.Count} bookmarks to metadata file");

                try {
                    Functions.UpdateMetadata(videoPath, metadata => {
                        metadata.bookmarks.AddRange(pending);
                    });
                    // bookmarks only ever get appended, so the saved ones are the first of the list;
                    // anything added while the file was being written stays for the next save
                    lock (bookmarkLock) bookmarks.RemoveRange(0, pending.Count);
                }
                catch (Exception e) {
                    Logger.WriteLine($"Bookmark status: Failed with exception {e.Message}");
                }
            }
        }
    }

    public class Bookmark {
        public enum BookmarkType {
            Manual,
            Kill,
            Death,
            Assist
        }
        public BookmarkType type { get; set; }
        public double time { get; set; }
    }
}