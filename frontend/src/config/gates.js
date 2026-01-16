export const ENTRY_GATES = [
  // Luồng Ô tô: 2 Cổng (Cả thường + điện)
  { id: 'GATE-IN-CAR-01', label: 'Cổng Ô tô 01', vehicleGroup: 'CAR' },
  { id: 'GATE-IN-CAR-02', label: 'Cổng Ô tô 02', vehicleGroup: 'CAR' },

  // Luồng Xe máy: 3 Cổng (Cả xe máy + điện + đạp)
  { id: 'GATE-IN-MOTO-01', label: 'Cổng Xe máy 01', vehicleGroup: 'MOTORBIKE' },
  { id: 'GATE-IN-MOTO-02', label: 'Cổng Xe máy 02', vehicleGroup: 'MOTORBIKE' },
  { id: 'GATE-IN-MOTO-03', label: 'Cổng Xe máy 03', vehicleGroup: 'MOTORBIKE' }
];

export const EXIT_GATES = [
  // Luồng Ra Ô tô
  { id: 'GATE-OUT-CAR-01', label: 'Cổng ra Ô tô 01' },
  { id: 'GATE-OUT-CAR-02', label: 'Cổng ra Ô tô 02' },

  // Luồng Ra Xe máy
  { id: 'GATE-OUT-MOTO-01', label: 'Cổng ra Xe máy 01' },
  { id: 'GATE-OUT-MOTO-02', label: 'Cổng ra Xe máy 02' },
  { id: 'GATE-OUT-MOTO-03', label: 'Cổng ra Xe máy 03' }
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
