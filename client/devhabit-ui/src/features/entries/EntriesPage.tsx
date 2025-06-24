import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useEntries } from './useEntries';
import { Entry, EntrySource, EntriesResponse } from './types';
import { format } from 'date-fns';
import type { Link as HypermediaLink } from '../../types/api';

const EntryCard = ({
  entry,
  onDelete,
  onArchive,
  onUnarchive,
}: {
  entry: Entry;
  onDelete: () => void;
  onArchive: () => void;
  onUnarchive: () => void;
}) => {
  const navigate = useNavigate();
  const deleteLink = entry.links?.find(link => link.rel === 'delete');
  const archiveLink = entry.links?.find(link => link.rel === 'archive');
  const unarchiveLink = entry.links?.find(link => link.rel === 'un-archive');
  const updateLink = entry.links?.find(link => link.rel === 'update');

  return (
    <div className="max-w-4xl mx-auto p-6 bg-white rounded-lg shadow">
      <div className="flex justify-between items-start">
        <div>
          <h3 className="text-lg font-semibold">{entry.habit.name}</h3>
          <p className="text-sm text-gray-500">{format(new Date(entry.date), 'PPP')}</p>
          <p className="mt-2">Value: {entry.value}</p>
          {entry.notes && <p className="mt-2 text-sm text-gray-600">{entry.notes}</p>}
          <p className="mt-1 text-xs text-gray-500">Source: {EntrySource[entry.source]}</p>
        </div>
        <div className="flex items-center space-x-4">
          {updateLink && (
            <button
              onClick={() => navigate(`/entries/${entry.id}/edit`)}
              className="text-blue-600 hover:text-blue-800 cursor-pointer"
            >
              Edit
            </button>
          )}
          {archiveLink && (
            <button
              onClick={onArchive}
              className="text-gray-600 hover:text-gray-800 cursor-pointer"
            >
              Archive
            </button>
          )}
          {unarchiveLink && (
            <button
              onClick={onUnarchive}
              className="text-gray-600 hover:text-gray-800 cursor-pointer"
            >
              Unarchive
            </button>
          )}
          {deleteLink && (
            <button onClick={onDelete} className="text-red-600 hover:text-red-800 cursor-pointer">
              Delete
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export const EntriesPage: React.FC = () => {
  const navigate = useNavigate();
  const [entries, setEntries] = useState<Entry[]>([]);
  const [entriesResponse, setEntriesResponse] = useState<EntriesResponse | null>(null);
  const [nextPageLink, setNextPageLink] = useState<HypermediaLink | null>(null);
  const [prevPageLink, setPrevPageLink] = useState<HypermediaLink | null>(null);

  const { getEntriesCursor, deleteEntry, archiveEntry, unarchiveEntry, isLoading, error } =
    useEntries();

  useEffect(() => {
    loadEntries();
  }, []);

  const loadEntries = async (url?: string) => {
    const result = await getEntriesCursor({
      limit: 6,
      url,
    });
    if (result) {
      setEntries(prevEntries => [...prevEntries, ...result.items]);
      setEntriesResponse(result);
      setNextPageLink(result.links.find(l => l.rel === 'next-page') || null);
      setPrevPageLink(result.links.find(l => l.rel === 'previous-page') || null);
    }
  };

  const handlePageChange = async (link: HypermediaLink) => {
    await loadEntries(link.href);
  };

  const handleDelete = async (entry: Entry) => {
    const deleteLink = entry.links?.find(link => link.rel === 'delete');
    if (!deleteLink) {
      return;
    }

    const confirmed = window.confirm('Are you sure you want to delete this entry?');
    if (!confirmed) {
      return;
    }

    const success = await deleteEntry(deleteLink);
    if (success) {
      await loadEntries();
    }
  };

  const handleArchive = async (entry: Entry) => {
    const archiveLink = entry.links?.find(link => link.rel === 'archive');
    if (!archiveLink) {
      return;
    }

    const confirmed = window.confirm('Are you sure you want to archive this entry?');
    if (!confirmed) {
      return;
    }

    const result = await archiveEntry(archiveLink);
    if (result) {
      await loadEntries();
    }
  };

  const handleUnarchive = async (entry: Entry) => {
    const unarchiveLink = entry.links?.find(link => link.rel === 'un-archive');
    if (!unarchiveLink) {
      return;
    }

    const confirmed = window.confirm('Are you sure you want to unarchive this entry?');
    if (!confirmed) {
      return;
    }

    const result = await unarchiveEntry(unarchiveLink);
    if (result) {
      await loadEntries();
    }
  };

  if (error) {
    return <p className="text-red-500">{error}</p>;
  }

  if (isLoading && entries.length === 0) {
    return <p className="text-gray-500">Loading...</p>;
  }

  return (
    <div className="space-y-6 max-w-4xl mx-auto p-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-semibold">My Entries</h1>
        <div className="space-x-4">
          <button
            onClick={() => navigate('/entries/imports')}
            className="px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 cursor-pointer"
          >
            Import Entries
          </button>
          <button
            onClick={() => {
              const createBatchLink = entriesResponse?.links?.find(
                link => link.rel === 'create-batch'
              );
              navigate('/entries/create-batch', { state: { createBatchLink } });
            }}
            className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 cursor-pointer"
            disabled={!entriesResponse?.links?.some(link => link.rel === 'create-batch')}
          >
            Batch Create Entries
          </button>
          {entriesResponse?.links?.some(link => link.rel === 'create') && (
            <button
              onClick={() => navigate('/entries/create')}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 cursor-pointer"
            >
              Create Entry
            </button>
          )}
        </div>
      </div>

      <div className="space-y-4">
        {entries.map((entry: Entry) => (
          <EntryCard
            key={entry.id}
            entry={entry}
            onDelete={() => handleDelete(entry)}
            onArchive={() => handleArchive(entry)}
            onUnarchive={() => handleUnarchive(entry)}
          />
        ))}
      </div>

      <div className="flex justify-center gap-4 mt-6">
        {prevPageLink && (
          <button
            onClick={() => handlePageChange(prevPageLink)}
            className="px-4 py-2 text-blue-600 hover:text-blue-800 cursor-pointer"
            disabled={isLoading}
          >
            ← Previous
          </button>
        )}
        {nextPageLink && (
          <button
            onClick={() => handlePageChange(nextPageLink)}
            className="px-4 py-2 text-blue-600 hover:text-blue-800 cursor-pointer"
            disabled={isLoading}
          >
            Load more
          </button>
        )}
      </div>
    </div>
  );
};
