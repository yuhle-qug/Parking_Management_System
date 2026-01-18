export const adminCredentials = {
  username: 'admin',
  password: '123'
}

export const randomPlate = (prefix = '30TEST') => {
  const suffix = Math.floor(Math.random() * 10000)
  return `${prefix}-${suffix}`
}

export const randomPhone = () => `09${Math.floor(10000000 + Math.random() * 89999999)}`
