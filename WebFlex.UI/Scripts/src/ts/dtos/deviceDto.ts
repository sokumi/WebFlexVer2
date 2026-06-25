export type DeviceDto = {
    id: string;
    deviceCode?: string | null;
    deviceName?: string | null;
    deviceType?: string | null;
};

export type DeviceSummaryDto = {
    deviceId: string;
    deviceName?: string | null;
    subscriptionStatus?: string | null;
    todayInsertedCount?: number | null;
};