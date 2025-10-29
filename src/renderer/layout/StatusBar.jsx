import PropTypes from 'prop-types';

function StatusBar({ filePath, dirty, statusMessage }) {
  return (
    <footer className="status-bar">
      <div>
        <strong>File:</strong> {filePath || 'Untitled'} {dirty ? '● Unsaved' : '✓ Saved'}
      </div>
      <div>
        <strong>API:</strong> Offline
      </div>
      <div className="status-message">{statusMessage}</div>
    </footer>
  );
}

StatusBar.propTypes = {
  filePath: PropTypes.string,
  dirty: PropTypes.bool,
  statusMessage: PropTypes.string,
};

StatusBar.defaultProps = {
  filePath: null,
  dirty: false,
  statusMessage: 'Ready',
};

export default StatusBar;
