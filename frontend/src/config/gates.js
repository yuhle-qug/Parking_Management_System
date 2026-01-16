export const ENTRY_GATES = [
  { id: 'GATE-IN-01', label: 'Cổng ô tô 01', vehicleGroup: 'CAR' },
  { id: 'GATE-IN-02', label: 'Cổng xe máy 01', vehicleGroup: 'MOTORBIKE' },
  { id: 'GATE-IN-03', label: 'Cổng hỗn hợp 03', vehicleGroup: 'CAR' }
];

export const EXIT_GATES = [
  { id: 'GATE-OUT-01', label: 'Cổng ra 01' },
  { id: 'GATE-OUT-02', label: 'Cổng ra 02' },
  { id: 'GATE-OUT-03', label: 'Cổng ra 03' }
];

export const VEHICLE_GROUPS = [
  { key: 'CAR', label: 'Luồng ô tô' },
  { key: 'MOTORBIKE', label: 'Luồng xe máy' }
];

export const VEHICLE_OPTIONS = {
  CAR: [
    { value: 'CAR', label: 'Ô tô', icon: '🚗' },
    { value: 'ELECTRIC_CAR', label: 'Ô tô điện', icon: '⚡' }
  ],
  MOTORBIKE: [
    { value: 'MOTORBIKE', label: 'Xe máy', icon: '🛵' },
    { value: 'ELECTRIC_MOTORBIKE', label: 'Xe máy điện', icon: '🔋' },
    { value: 'BICYCLE', label: 'Xe đạp', icon: '🚲' }
  ]
};
