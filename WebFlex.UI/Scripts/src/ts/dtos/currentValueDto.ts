export type CurrentValueDto = {
    groupId?: string | null;
    tagId: string;
    value?: string | null;
    status?: string | number | null;
    sourceTimestamp?: string | null;
    receivedAt?: string | null;
    updatedAt?: string | null;
};