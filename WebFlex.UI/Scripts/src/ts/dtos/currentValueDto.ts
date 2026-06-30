export type CurrentValueDto = {
    groupId?: string | null;
    tagId: string;

    /**
     * opc_tag.description
     * 화면 표시명: 수집 항목 설정
     */
    collectionSetting?: string | null;

    value?: string | null;

    /**
     * currentvalue.cookie_value
     * 화면 표시명: 변환값
     */
    cookieValue?: string | null;

    status?: string | number | null;
    updateCount?: number | null;
    sourceTimestamp?: string | null;
    receivedAt?: string | null;
    updatedAt?: string | null;
};